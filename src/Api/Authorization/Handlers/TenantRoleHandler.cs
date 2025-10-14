using Microsoft.AspNetCore.Authorization;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Api.Extensions;
using OnlineCommunities.Application.Interfaces;

namespace OnlineCommunities.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler that checks if a user has a specific role within the current tenant.
/// This handler queries YOUR database (not Entra ID) for role assignments.
/// </summary>
public class TenantRoleHandler : AuthorizationHandler<TenantRoleRequirement>
{
    private readonly IRoleManagementService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantRoleHandler> _logger;

    public TenantRoleHandler(
        IRoleManagementService roleService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantRoleHandler> logger)
    {
        _roleService = roleService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRoleRequirement requirement)
    {
        // Get user ID from Entra ID claim
        var userId = context.User.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("Authorization failed: No user ID found in claims");
            context.Fail();
            return;
        }

        // Get tenant ID from HTTP context (set by TenantContextMiddleware)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("Authorization failed: No HTTP context available");
            context.Fail();
            return;
        }

        var tenantId = httpContext.GetTenantId();
        if (tenantId == null)
        {
            _logger.LogWarning("Authorization failed: No tenant ID found in context for user {UserId}", userId);
            context.Fail();
            return;
        }

        // Check role in YOUR database (not Entra ID)
        var hasRole = await _roleService.UserHasRoleInTenant(
            userId.Value,
            tenantId.Value,
            requirement.RoleName
        );

        if (hasRole)
        {
            _logger.LogDebug(
                "Authorization succeeded: User {UserId} has role {RoleName} in tenant {TenantId}",
                userId, requirement.RoleName, tenantId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization failed: User {UserId} does not have role {RoleName} in tenant {TenantId}",
                userId, requirement.RoleName, tenantId);
            context.Fail();
        }
    }
}

