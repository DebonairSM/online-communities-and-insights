# Infrastructure & DevOps

## Hosting Strategy

### Azure App Services (Recommended for Standard Workloads)

**Web App Configuration**:
- App Service Plan: Premium V3 tier (P1V3 minimum for production)
- Always On enabled for consistent performance
- Multiple instances for high availability (minimum 2)
- Auto-scale rules based on CPU and HTTP queue length
- Deployment slots for staging and blue-green deployments

**Pros**:
- Fully managed PaaS with automatic patching
- Built-in load balancing and auto-scaling
- Simple deployment model
- Lower operational overhead
- Cost-effective for moderate scale

**Cons**:
- Less control over underlying infrastructure
- Scaling limits compared to Kubernetes
- Vendor lock-in to Azure

### Azure Kubernetes Service (For High Scale or Microservices)

**Cluster Configuration**:
- Node pools with separate system and user workloads
- Cluster autoscaler for dynamic scaling
- Azure CNI networking for pod IPs
- Azure Monitor integration for container insights
- RBAC with Azure AD integration

**When to Use**:
- Microservices architecture with independent scaling needs
- Multi-region deployments with global load balancing
- Advanced deployment strategies (canary, progressive delivery)
- Polyglot environments with multiple runtime requirements

**Considerations**:
- Higher operational complexity
- Requires Kubernetes expertise
- More expensive due to management overhead

## Environment Structure

### Development (Dev)
**Purpose**: Active development and feature testing

**Configuration**:
- Shared database with dev data
- Redis cache (Basic tier)
- Service Bus (Standard tier)
- Application Insights with 90-day retention
- Deployed on commit to `develop` branch
- No SLA requirements

**Access**: All developers

### QA/Test
**Purpose**: Quality assurance and integration testing

**Configuration**:
- Separate database with sanitized production data
- Redis cache (Standard tier)
- Service Bus (Standard tier)
- Application Insights with 180-day retention
- Deployed on PR merge to `develop`
- Load testing environment available

**Access**: QA team, developers (read-only for production-like troubleshooting)

### Staging
**Purpose**: Pre-production validation and client demos

**Configuration**:
- Production-like infrastructure (scaled down)
- Redis cache (Premium tier, single node)
- Service Bus (Premium tier)
- Application Insights with 1-year retention
- Deployed on commit to `release/*` branches
- Uptime SLA: 99%

**Access**: Product team, select clients for UAT

### Production
**Purpose**: Live customer-facing environment

**Configuration**:
- Full production infrastructure
- Redis cache (Premium tier, with clustering)
- Service Bus (Premium tier, with geo-replication)
- Application Insights with 2-year retention
- SQL Database geo-replication enabled
- Multi-region deployment (primary + DR)
- Uptime SLA: 99.9%

**Access**: Operations team (with approval workflows for changes)

## CI/CD Pipelines

### Source Control Strategy

**Branch Strategy** (GitFlow variant):
- `main`: Production-ready code
- `develop`: Integration branch for features
- `feature/*`: Individual feature branches
- `release/*`: Release preparation branches
- `hotfix/*`: Emergency production fixes

**Commit Conventions**:
- Conventional Commits format: `type(scope): description`
- Types: feat, fix, docs, style, refactor, test, chore
- Automatic changelog generation from commits

### GitHub Actions Pipelines

#### Build and Test Pipeline

```yaml
name: Build and Test

on:
  push:
    branches: [develop, main]
  pull_request:
    branches: [develop, main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Test
        run: dotnet test --no-build --configuration Release --logger "trx;LogFileName=test-results.trx"
      
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: '**/test-results.trx'
          reporter: dotnet-trx
      
      - name: Code coverage
        run: |
          dotnet test --collect:"XPlat Code Coverage"
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
```

#### Deploy to Dev Pipeline

```yaml
name: Deploy to Dev

on:
  push:
    branches: [develop]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: development
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_DEV }}
      
      - name: Build and push Docker image
        run: |
          docker build -t ${{ secrets.ACR_NAME }}.azurecr.io/api:${{ github.sha }} .
          docker push ${{ secrets.ACR_NAME }}.azurecr.io/api:${{ github.sha }}
      
      - name: Deploy to App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.APP_SERVICE_NAME_DEV }}
          images: ${{ secrets.ACR_NAME }}.azurecr.io/api:${{ github.sha }}
      
      - name: Run database migrations
        run: |
          dotnet ef database update --connection "${{ secrets.DB_CONNECTION_STRING_DEV }}"
```

#### Deploy to Production Pipeline

```yaml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_PROD }}
      
      - name: Deploy to staging slot
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.APP_SERVICE_NAME_PROD }}
          slot-name: staging
          images: ${{ secrets.ACR_NAME }}.azurecr.io/api:${{ github.sha }}
      
      - name: Run smoke tests on staging slot
        run: |
          npm install
          npm run test:smoke -- --url https://${{ secrets.APP_SERVICE_NAME_PROD }}-staging.azurewebsites.net
      
      - name: Swap slots (blue-green deployment)
        run: |
          az webapp deployment slot swap \
            --resource-group ${{ secrets.RESOURCE_GROUP_PROD }} \
            --name ${{ secrets.APP_SERVICE_NAME_PROD }} \
            --slot staging \
            --target-slot production
      
      - name: Monitor deployment
        run: |
          sleep 60
          ./scripts/check-health.sh https://${{ secrets.APP_SERVICE_NAME_PROD }}.azurewebsites.net
      
      - name: Rollback on failure
        if: failure()
        run: |
          az webapp deployment slot swap \
            --resource-group ${{ secrets.RESOURCE_GROUP_PROD }} \
            --name ${{ secrets.APP_SERVICE_NAME_PROD }} \
            --slot production \
            --target-slot staging
```

### Azure DevOps Pipelines (Alternative)

**Build Pipeline (azure-pipelines-build.yml)**:
```yaml
trigger:
  branches:
    include:
      - develop
      - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build solution'
  inputs:
    command: 'build'
    arguments: '--configuration $(buildConfiguration) --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

## Infrastructure as Code

### Bicep (Recommended)

**Main Infrastructure File (main.bicep)**:
```bicep
@description('Environment name (dev, qa, staging, prod)')
param environmentName string

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Unique identifier for this deployment')
param deploymentId string = uniqueString(resourceGroup().id)

var appServiceName = 'app-communities-${environmentName}-${deploymentId}'
var sqlServerName = 'sql-communities-${environmentName}-${deploymentId}'
var redisCacheName = 'redis-communities-${environmentName}-${deploymentId}'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'plan-communities-${environmentName}'
  location: location
  sku: {
    name: environmentName == 'prod' ? 'P2V3' : 'P1V3'
    capacity: environmentName == 'prod' ? 3 : 2
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SqlConnectionString)'
        }
        {
          name: 'Redis__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/RedisConnectionString)'
        }
      ]
    }
  }
}

// Staging slot for production
resource stagingSlot 'Microsoft.Web/sites/slots@2022-03-01' = if (environmentName == 'prod') {
  parent: appService
  name: 'staging'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled' // Use private endpoint
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'communities'
  location: location
  sku: {
    name: environmentName == 'prod' ? 'BC_Gen5_4' : 'S2'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000 // 250 GB
    zoneRedundant: environmentName == 'prod'
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2022-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: environmentName == 'prod' ? 'Premium' : 'Standard'
      family: environmentName == 'prod' ? 'P' : 'C'
      capacity: environmentName == 'prod' ? 1 : 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: 'sb-communities-${environmentName}-${deploymentId}'
  location: location
  sku: {
    name: environmentName == 'prod' ? 'Premium' : 'Standard'
  }
  properties: {
    zoneRedundant: environmentName == 'prod'
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-comm-${environmentName}-${deploymentId}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-communities-${environmentName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: environmentName == 'prod' ? 730 : 180
  }
}

output appServiceId string = appService.id
output keyVaultName string = keyVault.name
```

**Deployment Commands**:
```bash
# Create resource group
az group create --name rg-communities-prod --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-communities-prod \
  --template-file main.bicep \
  --parameters environmentName=prod

# Update with parameter file
az deployment group create \
  --resource-group rg-communities-prod \
  --template-file main.bicep \
  --parameters @prod.parameters.json
```

### Terraform (Alternative)

**Main Configuration (main.tf)**:
```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate"
    container_name       = "tfstate"
    key                  = "prod.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_app_service_plan" "main" {
  name                = "plan-communities-${var.environment}"
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  kind                = "Linux"
  reserved            = true

  sku {
    tier = var.environment == "prod" ? "PremiumV3" : "PremiumV3"
    size = var.environment == "prod" ? "P2V3" : "P1V3"
  }
}

resource "azurerm_app_service" "main" {
  name                = "app-communities-${var.environment}-${random_id.deployment.hex}"
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  app_service_plan_id = azurerm_app_service_plan.main.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on        = true
    linux_fx_version = "DOTNETCORE|8.0"
    min_tls_version  = "1.2"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.environment
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.main.instrumentation_key
  }
}
```

## Naming Conventions

**Azure Resource Naming**:
- Resource groups: `rg-{service}-{environment}`
- App Services: `app-{service}-{environment}-{uniqueid}`
- SQL Servers: `sql-{service}-{environment}-{uniqueid}`
- Storage accounts: `st{service}{environment}{uniqueid}` (no hyphens, max 24 chars)
- Key Vault: `kv-{service}-{environment}-{uniqueid}`
- Service Bus: `sb-{service}-{environment}-{uniqueid}`

**Tags** (applied to all resources):
- Environment: dev | qa | staging | prod
- Project: online-communities
- CostCenter: engineering
- Owner: team-platform
- ManagedBy: terraform | bicep

## Secrets and Key Management

### Azure Key Vault

**Secret Organization**:
- SQL connection strings
- Redis connection strings
- Service Bus connection strings
- Azure Communication Services connection strings
- Third-party API keys (if using external services)
- JWT signing keys
- OAuth client secrets

**Access Policies**:
- App Service managed identity has `Get` and `List` permissions
- DevOps service principal has `Set`, `Delete`, `Get`, `List` permissions
- Developers have `Get` and `List` for non-production environments

**Key Rotation**:
- Automated rotation for database credentials (90-day cycle)
- Manual rotation for third-party API keys (annual or on compromise)
- Version management for secrets (previous versions retained)

**Reference in Application**:
```csharp
var keyVaultUrl = configuration["KeyVault:Url"];
var credential = new DefaultAzureCredential();
var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

var sqlConnectionSecret = await secretClient.GetSecretAsync("SqlConnectionString");
var connectionString = sqlConnectionSecret.Value.Value;
```

## Deployment Strategy

### Blue-Green Deployment (App Service Slots)

**Process**:
1. Deploy new version to staging slot
2. Run automated smoke tests against staging
3. Warm up staging slot (pre-load cache, initialize connections)
4. Swap staging and production slots (atomic operation, 0-downtime)
5. Monitor production for errors
6. Rollback via swap if issues detected

**Benefits**:
- Zero-downtime deployments
- Instant rollback capability
- Testing in production-like environment

### Canary Deployment (Traffic Manager)

**Process**:
1. Deploy new version to canary App Service instance
2. Route 5% of traffic to canary
3. Monitor error rates and performance metrics
4. Gradually increase traffic (5% → 25% → 50% → 100%)
5. Rollback by removing canary from traffic manager

**Use Cases**:
- High-risk changes
- Major version upgrades
- New feature releases with unknown performance impact

## Observability and Alerting

### Application Insights

**Telemetry Collection**:
- Request/response tracking with timing
- Dependency calls (SQL, Redis, Service Bus, HTTP)
- Exceptions with stack traces
- Custom events for business metrics
- Performance counters (CPU, memory, GC)

**Kusto Queries**:
```kql
// Error rate over time
requests
| where timestamp > ago(1h)
| summarize total = count(), failures = countif(success == false) by bin(timestamp, 5m)
| extend errorRate = (failures * 100.0) / total
| render timechart

// Slow queries
dependencies
| where type == "SQL"
| where duration > 1000
| project timestamp, name, duration, resultCode
| order by duration desc

// Top exceptions
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
| take 10
```

### Log Analytics Workspace

**Log Aggregation**:
- Application logs from all services
- IIS/Kestrel access logs
- Azure resource diagnostic logs
- Custom structured logs via Serilog

**Retention**:
- Dev: 90 days
- QA: 180 days
- Staging: 365 days
- Production: 730 days

### Dashboards

**Operations Dashboard** (Application Insights Workbook):
- Request rate, duration, failure rate
- Dependency health (SQL, Redis, Service Bus)
- Top exceptions and errors
- Server health metrics

**Business Metrics Dashboard**:
- Posts created per hour
- Survey responses submitted
- Active users (current + 24h trend)
- Tenant-specific activity metrics

**Cost Dashboard** (Azure Cost Management):
- Cost by resource type
- Cost by environment
- Month-over-month trends
- Budget alerts

### Alerts

**Critical Alerts** (PagerDuty/OpsGenie integration):
- Error rate > 5% for 5 minutes
- Response time > 3 seconds (95th percentile) for 10 minutes
- Database DTU > 90% for 5 minutes
- Service Bus queue depth > 1000 messages
- Dependency failure (SQL, Redis unavailable)

**Warning Alerts** (Email/Slack):
- Error rate > 1% for 15 minutes
- CPU > 80% for 15 minutes
- Memory > 85% for 15 minutes
- Disk space < 10% free

## Disaster Recovery

### Backup Strategy

**SQL Database**:
- Automated backups with 35-day retention (production)
- Point-in-time restore capability
- Long-term retention (weekly for 1 year, monthly for 5 years)
- Geo-redundant backup storage

**Blob Storage**:
- Geo-redundant storage (GRS) for production
- Soft delete enabled (30-day retention)
- Versioning enabled for critical containers

**Redis Cache**:
- AOF persistence enabled (production)
- RDB snapshots every 6 hours
- Backup to Blob Storage

### Geo-Replication

**SQL Database**:
- Active geo-replication to secondary region
- Automatic failover group with read-write listener
- Read-only endpoint for secondary region

**Cosmos DB**:
- Multi-region writes for low latency
- Automatic failover to secondary regions
- Consistency level: Session (default)

### Failover Procedures

**Planned Maintenance**:
1. Notify users of maintenance window
2. Enable read-only mode or maintenance page
3. Perform geo-failover to secondary region
4. Execute maintenance on primary
5. Geo-failback to primary region
6. Resume normal operations

**Unplanned Outage**:
1. Monitor detects primary region failure
2. Automatic failover to secondary (SQL, Cosmos)
3. Manual DNS update to point to secondary App Service
4. Monitor secondary for stability
5. Investigate and resolve primary region issue
6. Plan geo-failback when safe

**Recovery Time Objective (RTO)**: 1 hour
**Recovery Point Objective (RPO)**: 5 minutes

## Security Hardening

### Network Security

**Virtual Network Integration**:
- App Service VNet integration for outbound traffic
- Private endpoints for SQL, Redis, Storage
- Network security groups (NSGs) for subnet isolation
- Azure Firewall for centralized egress control

**Access Restrictions**:
- IP allowlisting for admin endpoints
- Azure Front Door for DDoS protection
- WAF (Web Application Firewall) with OWASP ruleset

### Identity and Access

**Azure AD Integration**:
- Managed identities for Azure resource access
- Service principals for CI/CD pipelines
- Role-based access control (RBAC) for resource management
- Privileged Identity Management for admin access

**Least Privilege**:
- App Service: No direct internet access, only through Front Door
- SQL: No public endpoint, only private endpoint access
- Key Vault: RBAC with specific secret permissions
- Storage: Shared access signatures (SAS) with expiration

### Compliance

**Azure Policy**:
- Require tags on all resources
- Enforce TLS 1.2 minimum
- Require encryption at rest
- Deny public IP addresses on VMs
- Require Network Security Groups on subnets

**Security Center**:
- Continuous security assessment
- Vulnerability scanning
- Compliance dashboard (SOC 2, GDPR, ISO 27001)
- Security recommendations and remediation

## Cost Optimization

**Strategies**:
- Auto-shutdown for non-production environments (nights and weekends)
- Reserved instances for production workloads (1-year commitment)
- Spot instances for batch processing workloads
- Right-sizing based on actual utilization metrics
- Blob Storage lifecycle management (hot → cool → archive)
- SQL Database serverless tier for low-usage databases

**Monitoring**:
- Azure Cost Management + Billing
- Budget alerts at 50%, 75%, 90%, 100% of monthly budget
- Cost allocation by environment and tenant (via tags)
- Monthly cost review and optimization recommendations

