using Microsoft.AspNetCore.Authorization;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Api.Extensions;

namespace OnlineCommunities.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler that checks if the user owns the resource they're trying to access.
/// The resource must be passed in the authorization context.
/// </summary>
public class ResourceOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    private readonly ILogger<ResourceOwnerHandler> _logger;

    public ResourceOwnerHandler(ILogger<ResourceOwnerHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("Resource ownership check failed: No user ID in claims");
            context.Fail();
            return Task.CompletedTask;
        }

        // The resource should be passed as context.Resource
        // For example, if checking ownership of a Post:
        // var post = context.Resource as Post;
        // if (post != null && post.AuthorId == userId)
        // {
        //     context.Succeed(requirement);
        // }

        // TODO: Implement based on your entity types
        // This is a placeholder that will need to be enhanced based on actual resources
        
        _logger.LogDebug("Resource ownership check for user {UserId}", userId);
        
        // For now, this is a stub that always fails
        // You'll need to check the specific resource type and ownership
        context.Fail();
        
        return Task.CompletedTask;
    }
}

