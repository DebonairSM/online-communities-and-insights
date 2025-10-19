# Azure Resource Naming Conventions

## Overview

This document defines the standardized naming conventions for Azure resources in the Online Communities platform. Consistent naming ensures better resource management, cost tracking, and operational efficiency.

## General Principles

- **Consistency**: All resources follow the same pattern
- **Clarity**: Names clearly indicate purpose and environment
- **Length**: Stay within Azure resource name length limits
- **Uniqueness**: Ensure global uniqueness where required
- **Automation**: Support automated resource creation and management

## Naming Pattern

```
{Environment}-{Project}-{Component}-{Instance}-{Location}
```

### Components

| Component | Description | Examples |
|-----------|-------------|----------|
| Environment | Deployment environment | `dev`, `test`, `staging`, `prod` |
| Project | Project identifier | `oc` (Online Communities) |
| Component | Resource type/purpose | `api`, `db`, `storage`, `kv`, `sb` |
| Instance | Specific instance identifier | `01`, `02`, `primary`, `secondary` |
| Location | Azure region abbreviation | `eastus`, `westus2`, `europe` |

## Resource-Specific Conventions

### Compute Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| App Service | `{env}-{project}-{component}-{instance}` | `prod-oc-api-01` |
| App Service Plan | `{env}-{project}-{component}-plan` | `prod-oc-api-plan` |
| Function App | `{env}-{project}-func-{instance}` | `prod-oc-func-01` |
| Container Instance | `{env}-{project}-aci-{instance}` | `prod-oc-aci-01` |

### Storage Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| Storage Account | `{env}{project}{component}{instance}` | `prodocstorage01` |
| Blob Container | `{component}-{instance}` | `media-01`, `logs-01` |
| File Share | `{component}-{instance}` | `data-01`, `backup-01` |

### Database Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| SQL Server | `{env}-{project}-sql-{instance}` | `prod-oc-sql-01` |
| SQL Database | `{env}-{project}-db-{instance}` | `prod-oc-db-01` |
| Cosmos DB | `{env}-{project}-cosmos-{instance}` | `prod-oc-cosmos-01` |
| Redis Cache | `{env}-{project}-redis-{instance}` | `prod-oc-redis-01` |

### Networking Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| Virtual Network | `{env}-{project}-vnet` | `prod-oc-vnet` |
| Subnet | `{env}-{project}-{component}-subnet` | `prod-oc-api-subnet` |
| Network Security Group | `{env}-{project}-{component}-nsg` | `prod-oc-api-nsg` |
| Load Balancer | `{env}-{project}-{component}-lb` | `prod-oc-api-lb` |

### Security Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| Key Vault | `{env}-{project}-kv-{instance}` | `prod-oc-kv-01` |
| Managed Identity | `{env}-{project}-{component}-mi` | `prod-oc-api-mi` |
| Application Registration | `{env}-{project}-{component}-app` | `prod-oc-api-app` |

### Messaging Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| Service Bus | `{env}-{project}-sb-{instance}` | `prod-oc-sb-01` |
| Event Hub | `{env}-{project}-eh-{instance}` | `prod-oc-eh-01` |
| Event Grid | `{env}-{project}-eg-{instance}` | `prod-oc-eg-01` |

### Monitoring Resources

| Resource Type | Pattern | Example |
|---------------|---------|---------|
| Application Insights | `{env}-{project}-ai-{instance}` | `prod-oc-ai-01` |
| Log Analytics Workspace | `{env}-{project}-law-{instance}` | `prod-oc-law-01` |
| Action Group | `{env}-{project}-{component}-ag` | `prod-oc-api-ag` |

## Environment Codes

| Environment | Code | Description |
|-------------|------|-------------|
| Development | `dev` | Local development environment |
| Testing | `test` | Automated testing environment |
| Staging | `staging` | Pre-production environment |
| Production | `prod` | Production environment |

## Location Codes

| Region | Code | Description |
|--------|------|-------------|
| East US | `eastus` | Primary region |
| West US 2 | `westus2` | Secondary region |
| West Europe | `westeurope` | European region |
| Southeast Asia | `southeastasia` | Asia Pacific region |

## Component Codes

| Component | Code | Description |
|-----------|------|-------------|
| API | `api` | Web API services |
| Database | `db` | Database services |
| Storage | `storage` | Storage services |
| Key Vault | `kv` | Key management |
| Service Bus | `sb` | Messaging services |
| Function | `func` | Serverless functions |
| Container | `aci` | Container instances |
| Cache | `redis` | Caching services |

## Special Considerations

### Length Limits

- Storage accounts: 3-24 characters, lowercase, alphanumeric
- Key Vaults: 3-24 characters, alphanumeric and hyphens
- App Services: 2-60 characters, alphanumeric and hyphens
- SQL Servers: 1-63 characters, alphanumeric and hyphens

### Global Uniqueness

Some resources require global uniqueness:
- Storage accounts
- Key Vaults
- Application registrations
- Function apps

Use additional suffixes or prefixes when needed.

### Reserved Words

Avoid Azure reserved words:
- `azure`
- `microsoft`
- `windows`
- `system`

## Examples

### Complete Resource Set

```
Environment: prod
Project: oc
Location: eastus

Resources:
- prod-oc-api-01 (App Service)
- prod-oc-api-plan (App Service Plan)
- prodocstorage01 (Storage Account)
- prod-oc-sql-01 (SQL Server)
- prod-oc-db-01 (SQL Database)
- prod-oc-kv-01 (Key Vault)
- prod-oc-sb-01 (Service Bus)
- prod-oc-ai-01 (Application Insights)
```

### Multi-Region Setup

```
Primary Region (eastus):
- prod-oc-api-01-eastus
- prod-oc-db-01-eastus

Secondary Region (westus2):
- prod-oc-api-01-westus2
- prod-oc-db-01-westus2
```

## Implementation

### Automated Naming

Use the `AzureResourceNamer` class to generate consistent names:

```csharp
var namer = new AzureResourceNamer("prod", "oc", "eastus");
var appServiceName = namer.GetAppServiceName("api", "01");
// Result: prod-oc-api-01
```

### Validation

All resource names should be validated against these conventions before creation.

### Documentation

Update this document when adding new resource types or changing conventions.

## Compliance

- All new resources must follow these naming conventions
- Existing resources should be renamed during maintenance windows
- Automated tools should validate naming compliance
- Regular audits ensure ongoing compliance
