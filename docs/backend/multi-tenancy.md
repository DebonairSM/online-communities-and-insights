# Multi-Tenancy Strategy

## Architecture Decision

**Shared Database with Composite Keys** - All tenant-scoped tables use `(TenantId, Id)` composite primary keys for strong isolation and referential integrity.

## Data Isolation

### Composite Key Pattern
```sql
CREATE TABLE Communities (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    CONSTRAINT PK_Communities PRIMARY KEY (TenantId, Id),
    CONSTRAINT FK_Communities_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id)
);
```

### Entity Framework Configuration
```csharp
builder.Entity<Community>().HasKey(x => new { x.TenantId, x.Id });
builder.Entity<Community>()
    .HasOne(p => p.Tenant)
    .WithMany()
    .HasForeignKey(p => p.TenantId);
```

## Application-Level Isolation

### Global Query Filters
```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Post>().HasQueryFilter(p => p.TenantId == _tenantId);
    builder.Entity<Survey>().HasQueryFilter(s => s.TenantId == _tenantId);
}
```

### Claims-Based Tenant Context
```csharp
public static Guid? GetTenantId(this ClaimsPrincipal principal)
{
    var tenantIdClaim = principal.FindFirst("tenantId") 
                      ?? principal.FindFirst("extension_TenantId");
    return Guid.TryParse(tenantIdClaim?.Value, out var tid) ? tid : null;
}
```

## Database-Level Security

### Row-Level Security Policies
```sql
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(TenantId)
ON dbo.Posts,
ON dbo.Surveys,
ON dbo.Communities
WITH (STATE = ON);
```

## Tenant Management

### Provisioning Flow
1. Create tenant record
2. Apply default configuration
3. Create admin user and assign roles
4. Set up billing account

### Feature Flags
Per-tenant feature enablement and A/B testing capabilities.

### Customization
- JSON columns for tenant-specific attributes
- Branding configuration (logos, colors, themes)
- Custom subdomains or domains
