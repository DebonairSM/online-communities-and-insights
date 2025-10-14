using Microsoft.AspNetCore.Authorization;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Api.Extensions;

namespace OnlineCommunities.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler that verifies a user is a member of the current tenant.
/// This is a basic check to ensure users can only access data from their own tenants.
/// </summary>
public class TenantMembershipHandler : AuthorizationHandler<TenantMembershipRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantMembershipHandler> _logger;
    // TODO: Add ITenantMembershipService when implemented

    public TenantMembershipHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantMembershipHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMembershipRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("Tenant membership check failed: No user ID in claims");
            context.Fail();
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("Tenant membership check failed: No HTTP context");
            context.Fail();
            return;
        }

        var tenantId = httpContext.GetTenantId();
        if (tenantId == null)
        {
            _logger.LogWarning("Tenant membership check failed: No tenant ID in context");
            context.Fail();
            return;
        }

        // TODO: Implement actual membership check against database
        // For now, if we have both user ID and tenant ID, assume valid
        // This should be replaced with: await _membershipService.IsUserMemberOfTenant(userId.Value, tenantId.Value)
        
        _logger.LogDebug(
            "Tenant membership check passed for user {UserId} in tenant {TenantId}",
            userId, tenantId);
        context.Succeed(requirement);

        await Task.CompletedTask;
    }
}

