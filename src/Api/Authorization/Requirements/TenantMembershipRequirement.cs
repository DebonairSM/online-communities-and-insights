using Microsoft.AspNetCore.Authorization;

namespace OnlineCommunities.Api.Authorization.Requirements;

/// <summary>
/// Authorization requirement that checks if the user is a member of the current tenant.
/// This ensures users can only access data from tenants they belong to.
/// </summary>
public class TenantMembershipRequirement : IAuthorizationRequirement
{
    // No properties needed - just checks if user is a member
}

