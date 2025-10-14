# ADR-005: Shared Database with Composite Keys for Multi-Tenancy

**Status:** Accepted  
**Date:** 2025-10-13

## Context

The Insight Community Platform must support secure, scalable multi-tenancy. We evaluated several data isolation patterns:

- Shared database with discriminator (TenantId column)
- Schema-per-tenant
- Database-per-tenant

We require:

- Strong tenant isolation (no cross-tenant data leaks)
- Cost efficiency for many tenants
- Simplicity in operations and migrations
- Support for cross-tenant analytics (platform metrics)

## Decision

We will use a **shared database** with a **composite primary key** `(TenantId, Id)` on all tenant-scoped tables. This is enforced at both the application and database level.

### Key Points

- Every tenant-scoped table includes a `TenantId` column (NOT NULL)
- Composite primary keys: `(TenantId, Id)`
- All foreign keys include `TenantId` to enforce same-tenant relationships
- Unique constraints and indexes are always scoped to `TenantId`
- EF Core global query filters and repository patterns ensure all queries are tenant-scoped
- SQL Server Row-Level Security (RLS) is enabled for defense in depth

## Example Table Definitions

```sql
CREATE TABLE Communities (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ...
    CONSTRAINT PK_Communities PRIMARY KEY (TenantId, Id),
    CONSTRAINT UQ_Communities_Tenant_Slug UNIQUE (TenantId, Slug)
);

CREATE TABLE Posts (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    CommunityId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    ...
    CONSTRAINT PK_Posts PRIMARY KEY (TenantId, Id),
    CONSTRAINT FK_Posts_Communities FOREIGN KEY (TenantId, CommunityId)
        REFERENCES Communities (TenantId, Id) ON DELETE CASCADE
);
```

## Example EF Core Model

```csharp
public class Community : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    ...
}

public class Post : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    ...
}

// In OnModelCreating
builder.Entity<Community>().HasKey(x => new { x.TenantId, x.Id });
builder.Entity<Post>().HasKey(x => new { x.TenantId, x.Id });
builder.Entity<Post>()
    .HasOne(p => p.Community)
    .WithMany(c => c.Posts)
    .HasForeignKey(p => new { p.TenantId, p.CommunityId });
```

## Benefits

- **Strong tenant isolation**: DB prevents cross-tenant references
- **Cost efficiency**: One database, shared resources
- **Operational simplicity**: Single schema, easy migrations
- **Performance**: Indexes lead with TenantId for partition elimination
- **Cross-tenant analytics**: Still possible for platform metrics

## Drawbacks

- Composite keys everywhere (more verbose code)
- Schema changes affect all tenants
- Noisy neighbor risk (mitigated by quotas and RLS)

## Consequences

- All new tenant-scoped tables and code must use composite keys
- All queries and repository methods must include TenantId
- Migrations must always add TenantId and composite keys
- Test coverage must include multi-tenant isolation scenarios

## Alternatives Considered

- **Schema-per-tenant**: More isolation, but higher operational complexity
- **Database-per-tenant**: Maximum isolation, but not cost-effective for our scale
- **Single-column PK + TenantId**: Simpler, but DB can't enforce same-tenant FKs

## References

- [Backend Architecture: Multi-Tenant Strategy](../../contexts/backend-architecture.md#multi-tenant-strategy)
- [Security Model: Tenant Isolation](../../contexts/security-model.md#tenant-isolation)
- [EF Core Docs: Composite Keys](https://learn.microsoft.com/ef/core/modeling/keys?tabs=data-annotations#composite-keys)
 - [SQL Server Docs: Row-Level Security](https://learn.microsoft.com/sql/relational-databases/security/row-level-security)
