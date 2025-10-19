using Microsoft.AspNetCore.Authorization;

namespace OnlineCommunities.Api.Authorization.Requirements;

/// <summary>
/// Authorization requirement that checks if the user has a specific role within a tenant.
/// This is used to enforce role-based access control at the tenant level.
/// </summary>
public class TenantRoleRequirement : IAuthorizationRequirement
{
    public string RoleName { get; }

    public TenantRoleRequirement(string roleName)
    {
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
    }
}

