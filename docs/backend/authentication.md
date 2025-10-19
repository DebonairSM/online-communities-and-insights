# Authentication & Security

## Authentication Strategy

**Microsoft Entra External ID** for managed identity with custom tenant/role claims.

### Current Implementation Status ✅

**Completed Components**:
- JWT Bearer authentication configured for Entra External ID token validation
- ApplicationDbContext with User, Tenant, and TenantMembership entities
- UserRepository and TenantMembershipRepository implementations
- ClaimsPrincipalExtensions for extracting tenant and role information
- Authorization handlers for multi-tenant role-based access control
- AuthController with token validation endpoints

**Configuration Required**:
- Microsoft Entra External ID tenant setup in Azure portal
- API Connector configuration for token enrichment
- Custom attributes setup (`extension_TenantId`, `extension_Roles`)

### Token Structure
```json
{
  "sub": "entra-user-guid",
  "email": "user@example.com",
  "extension_TenantId": "tenant-guid",
  "extension_Roles": ["Member", "Admin"]
}
```

### API Connector for Claims Enrichment
```csharp
[HttpPost("token-enrichment")]
public async Task<IActionResult> EnrichToken([FromBody] EntraTokenRequest request)
{
    var user = await _userRepo.GetByEmailAsync(request.Email);
    var membership = await _tenantMembershipRepo.GetPrimaryForUserAsync(user.Id);
    
    return Ok(new {
        TenantId = membership?.TenantId.ToString(),
        Roles = new[] { membership?.RoleName ?? "Member" }
    });
}
```

## Authorization

### Role-Based Access Control
**System Roles**: `PlatformAdmin`, `TenantAdmin`, `Moderator`, `Member`, `Guest`

**Permission-Based**: Granular permissions like `community.create`, `post.delete`, `survey.publish`

### Multi-Tenant Authorization
```csharp
[Authorize(Policy = "RequireSameTenant")]
public async Task<IActionResult> DeletePost(Guid postId) 
{
    // Tenant isolation enforced automatically
}
```

### Claims Extraction
```csharp
// Get tenant ID from Entra External ID custom claim
public static Guid? GetTenantId(this ClaimsPrincipal principal)
{
    var tenantIdClaim = principal.FindFirst("extension_TenantId");
    if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
    {
        return tenantId;
    }
    return null;
}

// Get user roles from Entra External ID custom claims
public static string[] GetRoles(this ClaimsPrincipal principal)
{
    return principal.FindAll("extension_Roles").Select(c => c.Value).ToArray();
}

// Get Entra External ID subject for user identification
public static string? GetEntraIdSubject(this ClaimsPrincipal principal)
{
    return principal.FindFirst("sub")?.Value;
}
```

## Data Protection

### Tenant Isolation
- Composite primary keys `(TenantId, Id)`
- Global query filters in EF Core
- Row-level security policies in SQL Server

```sql
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(TenantId)
ON dbo.Posts, dbo.Communities
WITH (STATE = ON);
```

### Sensitive Data Handling
- Passwords hashed with bcrypt (work factor 12)
- PII encryption at rest using Azure Key Vault
- Audit logging for all data access

## Security Headers & Middleware

```csharp
app.UseSecurityHeaders(policies => policies
    .AddFrameOptionsDeny()
    .AddContentTypeOptionsNoSniff()
    .AddStrictTransportSecurityMaxAge(31536000));
```
