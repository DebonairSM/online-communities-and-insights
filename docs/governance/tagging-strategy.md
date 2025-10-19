# Azure Resource Tagging Strategy

## Overview

This document defines the standardized tagging strategy for Azure resources in the Online Communities platform. Consistent tagging enables better cost management, resource organization, and compliance tracking.

## Tagging Principles

- **Consistency**: All resources must have the same core tags
- **Automation**: Tags should be applied automatically during resource creation
- **Compliance**: Tags support governance and compliance requirements
- **Cost Management**: Tags enable accurate cost allocation and tracking
- **Operations**: Tags support operational processes and incident response

## Required Tags

All resources must have the following mandatory tags:

| Tag Name | Description | Example Values | Required |
|----------|-------------|----------------|----------|
| `Environment` | Deployment environment | `dev`, `test`, `staging`, `prod` | Yes |
| `Project` | Project identifier | `oc` (Online Communities) | Yes |
| `Component` | Resource component/purpose | `api`, `database`, `storage`, `messaging` | Yes |
| `Owner` | Team or individual responsible | `platform-team`, `backend-team` | Yes |
| `CostCenter` | Cost allocation center | `engineering`, `product`, `infrastructure` | Yes |
| `CreatedDate` | Resource creation date | `2025-01-15` | Yes |
| `CreatedBy` | Who created the resource | `azure-devops`, `terraform`, `manual` | Yes |

## Optional Tags

The following tags are recommended for enhanced management:

| Tag Name | Description | Example Values | Required |
|----------|-------------|----------------|----------|
| `Application` | Application name | `online-communities-api` | No |
| `Version` | Application version | `1.0.0`, `2.1.3` | No |
| `DataClassification` | Data sensitivity level | `public`, `internal`, `confidential`, `restricted` | No |
| `BackupRequired` | Whether backup is required | `true`, `false` | No |
| `RetentionPeriod` | Data retention period | `30d`, `90d`, `1y`, `7y` | No |
| `Compliance` | Compliance requirements | `gdpr`, `sox`, `hipaa`, `pci` | No |
| `MaintenanceWindow` | Preferred maintenance window | `sunday-2am`, `saturday-4am` | No |
| `ContactEmail` | Contact for resource issues | `team@company.com` | No |
| `Documentation` | Link to documentation | `https://wiki.company.com/resource` | No |

## Environment-Specific Tags

### Development Environment
```
Environment: dev
Project: oc
Component: api
Owner: development-team
CostCenter: engineering
DataClassification: internal
BackupRequired: false
```

### Testing Environment
```
Environment: test
Project: oc
Component: api
Owner: qa-team
CostCenter: engineering
DataClassification: internal
BackupRequired: false
```

### Staging Environment
```
Environment: staging
Project: oc
Component: api
Owner: platform-team
CostCenter: engineering
DataClassification: internal
BackupRequired: true
```

### Production Environment
```
Environment: prod
Project: oc
Component: api
Owner: platform-team
CostCenter: product
DataClassification: confidential
BackupRequired: true
Compliance: gdpr
RetentionPeriod: 7y
```

## Component-Specific Tags

### API Services
```
Component: api
Application: online-communities-api
Version: 1.2.0
MaintenanceWindow: sunday-2am
ContactEmail: api-team@company.com
```

### Database Services
```
Component: database
Application: online-communities-db
DataClassification: confidential
BackupRequired: true
RetentionPeriod: 7y
Compliance: gdpr
```

### Storage Services
```
Component: storage
Application: online-communities-storage
DataClassification: confidential
BackupRequired: true
RetentionPeriod: 1y
```

### Messaging Services
```
Component: messaging
Application: online-communities-messaging
DataClassification: internal
BackupRequired: false
```

## Tag Validation Rules

### Tag Name Rules
- Use PascalCase for tag names
- No spaces or special characters except hyphens
- Maximum 50 characters
- Must start with a letter

### Tag Value Rules
- Use lowercase for values where appropriate
- No spaces in values (use hyphens instead)
- Maximum 256 characters
- Use consistent date format (YYYY-MM-DD)

### Required Tag Validation
- All required tags must be present
- Tag values must not be empty
- Environment must be one of: dev, test, staging, prod
- Project must match the project identifier
- Component must be a valid component type

## Implementation

### Automated Tagging

Use the `TaggingService` class to apply tags automatically:

```csharp
var taggingService = new TaggingService();
var tags = taggingService.GetStandardTags("prod", "oc", "api", "platform-team");
// Apply tags to resource
```

### Tag Templates

Pre-defined tag templates for common scenarios:

```csharp
// Production API service
var prodApiTags = TaggingService.GetProductionApiTags("platform-team");

// Development database
var devDbTags = TaggingService.GetDevelopmentDatabaseTags("dev-team");

// Staging storage
var stagingStorageTags = TaggingService.GetStagingStorageTags("qa-team");
```

### Validation

All resources must pass tag validation before deployment:

```csharp
var validationResult = TaggingService.ValidateTags(resourceTags);
if (!validationResult.IsValid)
{
    throw new InvalidOperationException($"Tag validation failed: {string.Join(", ", validationResult.Errors)}");
}
```

## Cost Management

### Cost Allocation

Tags enable accurate cost allocation:

- **By Environment**: Track costs per environment
- **By Component**: Track costs per system component
- **By Team**: Track costs per team/owner
- **By Project**: Track costs per project

### Cost Reports

Generate cost reports using tag filters:

```sql
-- Monthly costs by environment
SELECT Environment, SUM(Cost) 
FROM AzureCosts 
WHERE CreatedDate >= '2025-01-01' 
GROUP BY Environment

-- Costs by component
SELECT Component, SUM(Cost) 
FROM AzureCosts 
WHERE Environment = 'prod' 
GROUP BY Component
```

## Compliance and Governance

### Data Classification

Use `DataClassification` tag for data governance:

- **Public**: No restrictions
- **Internal**: Company internal use only
- **Confidential**: Restricted access required
- **Restricted**: Highly sensitive data

### Compliance Tracking

Use `Compliance` tag for regulatory requirements:

- **GDPR**: General Data Protection Regulation
- **SOX**: Sarbanes-Oxley Act
- **HIPAA**: Health Insurance Portability and Accountability Act
- **PCI**: Payment Card Industry

### Audit Requirements

Tags support audit and compliance:

- Track resource ownership
- Monitor data classification
- Ensure backup compliance
- Validate retention policies

## Operations

### Incident Response

Tags support incident response:

- Identify affected resources quickly
- Contact responsible teams
- Determine impact scope
- Track resolution progress

### Maintenance Windows

Use `MaintenanceWindow` tag for scheduling:

- Plan maintenance activities
- Avoid conflicts
- Coordinate with teams
- Minimize downtime

### Documentation

Use `Documentation` tag for resource documentation:

- Link to runbooks
- Reference architecture docs
- Point to troubleshooting guides
- Maintain knowledge base

## Monitoring and Alerting

### Tag-Based Alerts

Create alerts based on tags:

```yaml
# Alert for untagged resources
- name: UntaggedResources
  condition: Tags.Environment == null
  action: NotifyGovernanceTeam

# Alert for production changes
- name: ProductionChanges
  condition: Tags.Environment == "prod"
  action: RequireApproval
```

### Resource Discovery

Use tags for resource discovery:

```csharp
// Find all production API resources
var prodApiResources = await azureClient.Resources
    .Where(r => r.Tags["Environment"] == "prod" && r.Tags["Component"] == "api")
    .ToListAsync();

// Find resources by owner
var teamResources = await azureClient.Resources
    .Where(r => r.Tags["Owner"] == "platform-team")
    .ToListAsync();
```

## Best Practices

### Tag Naming
- Use consistent naming conventions
- Avoid abbreviations
- Use descriptive names
- Follow company standards

### Tag Values
- Use standardized values
- Avoid free-form text
- Use enums where possible
- Keep values concise

### Tag Management
- Apply tags at resource creation
- Validate tags before deployment
- Monitor tag compliance
- Regular tag audits

### Tag Lifecycle
- Update tags when resources change
- Remove obsolete tags
- Maintain tag documentation
- Train teams on tagging

## Compliance Checklist

- [ ] All resources have required tags
- [ ] Tag values follow naming conventions
- [ ] Data classification is accurate
- [ ] Compliance requirements are tagged
- [ ] Cost allocation is properly configured
- [ ] Documentation links are current
- [ ] Contact information is accurate
- [ ] Retention policies are specified

## Tools and Automation

### Azure Policy

Use Azure Policy to enforce tagging:

```json
{
  "if": {
    "allOf": [
      {
        "field": "tags['Environment']",
        "exists": "false"
      }
    ]
  },
  "then": {
    "effect": "deny"
  }
}
```

### Terraform

Apply tags in Terraform:

```hcl
resource "azurerm_resource_group" "example" {
  name     = "example-rg"
  location = "East US"
  
  tags = {
    Environment = "prod"
    Project     = "oc"
    Component   = "api"
    Owner       = "platform-team"
    CostCenter  = "product"
  }
}
```

### PowerShell

Apply tags using PowerShell:

```powershell
$tags = @{
    Environment = "prod"
    Project     = "oc"
    Component   = "api"
    Owner       = "platform-team"
    CostCenter  = "product"
}

Set-AzResource -ResourceId $resourceId -Tag $tags
```

## Review and Updates

This tagging strategy should be reviewed and updated:

- Quarterly for accuracy
- When new compliance requirements arise
- When new resource types are added
- When organizational changes occur

## Contact

For questions about this tagging strategy, contact the Platform Team at platform-team@company.com.
