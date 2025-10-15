using System.Security.Claims;
using System.Linq;

namespace OnlineCommunities.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to easily extract common claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the claims principal.
    /// Maps from Entra External ID 'sub' claim to local user ID via database lookup.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        // Extract user ID from Entra External ID 'sub' claim
        var subClaim = principal.FindFirst("sub");
        if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the Entra ID subject identifier from the claims principal.
    /// This is the unique identifier from Microsoft Entra ID.
    /// </summary>
    public static string? GetEntraIdSubject(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Gets the Entra External ID object identifier (OID) from the claims principal.
    /// This is used for mapping Entra External ID users to local user records.
    /// </summary>
    public static string? GetEntraOid(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("oid")?.Value;
    }

    /// <summary>
    /// Gets the user's email from the claims principal.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user's display name from the claims principal.
    /// </summary>
    public static string? GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the tenant ID from the claims principal.
    /// Reads from Entra External ID custom 'extension_TenantId' claim.
    /// </summary>
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal.FindFirst("extension_TenantId");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Gets the user roles from the claims principal.
    /// Reads from Entra External ID custom 'extension_Roles' claim collection.
    /// </summary>
    public static string[] GetRoles(this ClaimsPrincipal principal)
    {
        var customRoles = principal.FindAll("extension_Roles").Select(c => c.Value).ToArray();
        return customRoles;
    }
}

