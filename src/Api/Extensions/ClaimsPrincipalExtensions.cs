using System.Security.Claims;

namespace OnlineCommunities.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to easily extract common claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the claims principal.
    /// Looks for 'sub' claim (from Entra ID) or NameIdentifier claim.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
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
    /// This should be set by the authentication system after login.
    /// </summary>
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal.FindFirst("tenantId");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }
}

