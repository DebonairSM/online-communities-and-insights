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
    /// For Entra External ID: maps from 'oid' claim to local user ID via database lookup.
    /// For legacy tokens: looks for 'sub' claim or NameIdentifier claim.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        // Try Entra External ID 'oid' claim first
        var oid = principal.FindFirst("oid")?.Value;
        if (!string.IsNullOrEmpty(oid))
        {
            // Note: This requires a service to map Entra OID to local User.Id
            // For now, we'll extract from sub claim but this should be enhanced
            // with a proper mapping service in production
            var subClaim = principal.FindFirst("sub");
            if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
            {
                return userId;
            }
        }
        
        // Fallback to legacy 'sub' claim or NameIdentifier
        var subClaimFallback = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (subClaimFallback != null && Guid.TryParse(subClaimFallback.Value, out var userIdFallback))
        {
            return userIdFallback;
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
    /// For Entra External ID: reads from custom 'extension_TenantId' claim.
    /// For legacy tokens: reads from 'tenantId' claim.
    /// </summary>
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        // Try Entra External ID custom claim first
        var tenantIdClaim = principal.FindFirst("extension_TenantId") ?? principal.FindFirst("tenantId");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Gets the user roles from the claims principal.
    /// For Entra External ID: reads from custom 'extension_Roles' claim collection.
    /// For legacy tokens: reads from standard role claims.
    /// </summary>
    public static string[] GetRoles(this ClaimsPrincipal principal)
    {
        // Try Entra External ID custom claim first (string collection)
        var customRoles = principal.FindAll("extension_Roles").Select(c => c.Value).ToArray();
        if (customRoles.Length > 0)
        {
            return customRoles;
        }

        // Fallback to standard role claims
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    }
}

