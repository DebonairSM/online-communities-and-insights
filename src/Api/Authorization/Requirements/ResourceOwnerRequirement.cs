using Microsoft.AspNetCore.Authorization;

namespace OnlineCommunities.Api.Authorization.Requirements;

/// <summary>
/// Authorization requirement that checks if the user owns the resource they're trying to access.
/// This is used for operations like editing or deleting user-created content.
/// </summary>
public class ResourceOwnerRequirement : IAuthorizationRequirement
{
    // No properties needed - ownership is determined by the resource itself
}

