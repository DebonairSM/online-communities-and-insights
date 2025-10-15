# Domain Model

## Core Entities

### Tenant
Top-level organization (client using the platform).

**Key Attributes**: `Id`, `Name`, `Domain`, `Status`, `SubscriptionTier`  
**Relationships**: Has many `Communities`, `Users` (via memberships)  
**Rules**: Domain unique globally, cannot delete with active communities

### User  
Individual person using the platform.

**Key Attributes**: `Id`, `Email`, `FirstName`, `LastName`, `Status`  
**Relationships**: Has many `Memberships`, `Posts`, `SurveyResponses`  
**Rules**: Email unique globally, cannot delete with active content

### Community
Branded space within a tenant.

**Key Attributes**: `Id`, `TenantId`, `Name`, `Slug`, `Visibility`, `Status`  
**Relationships**: Belongs to `Tenant`, has many `Posts`, `Surveys`  
**Rules**: Slug unique within tenant

### Post
User-generated content in communities.

**Key Attributes**: `Id`, `TenantId`, `CommunityId`, `AuthorId`, `Title`, `Content`, `Status`  
**Relationships**: Belongs to `Community` and `User`, has many `Comments`  
**Rules**: Published posts cannot be deleted (soft delete only)

### Survey
Multi-question research instrument.

**Key Attributes**: `Id`, `TenantId`, `CommunityId`, `Title`, `Status`, `IsAnonymous`  
**Relationships**: Has many `Questions`, `SurveyResponses`  
**Rules**: Must have at least one question

### ResearchTask
Qualitative research activities (video diaries, photo tasks).

**Key Attributes**: `Id`, `TenantId`, `Title`, `TaskType`, `Status`, `RequirementsModeration`  
**Relationships**: Has many `TaskSubmissions`  
**Rules**: Cannot delete task with submissions

## Multi-Tenant Relationships

All tenant-scoped entities include:
- `TenantId` (Guid) - Foreign key for tenant isolation
- Composite primary key: `(TenantId, Id)`

### Membership Pattern
```csharp
public class TenantMembership
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string RoleName { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

## Domain Events

Key events for cross-service communication:
- `UserRegistered`, `CommunityCreated`, `PostCreated`
- `SurveyPublished`, `TaskCompleted`, `MemberJoined`

## Value Objects

**Email**: Validation and normalization  
**TenantSlug**: URL-safe identifier generation  
**SurveySettings**: Configuration aggregation