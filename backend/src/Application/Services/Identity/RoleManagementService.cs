using Microsoft.Extensions.Logging;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Core.Interfaces;

namespace OnlineCommunities.Application.Services.Identity;

/// <summary>
/// Service for managing user roles within tenants.
/// This is the implementation that the authorization handler uses.
/// It queries YOUR database, NOT Entra ID!
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly ITenantMembershipRepository _membershipRepository;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        ITenantMembershipRepository membershipRepository,
        ILogger<RoleManagementService> logger)
    {
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<bool> UserHasRoleInTenant(Guid userId, Guid tenantId, string roleName)
    {
        _logger.LogDebug(
            "Checking if user {UserId} has role {RoleName} in tenant {TenantId}",
            userId, roleName, tenantId);

        var hasRole = await _membershipRepository.UserHasRoleInTenantAsync(userId, tenantId, roleName);
        
        _logger.LogDebug(
            "User {UserId} {Result} role {RoleName} in tenant {TenantId}",
            userId, hasRole ? "has" : "does not have", roleName, tenantId);

        return hasRole;
    }

    public async Task<List<string>> GetUserRolesInTenant(Guid userId, Guid tenantId)
    {
        return await _membershipRepository.GetUserRolesInTenantAsync(userId, tenantId);
    }

    public async Task AssignRoleToUser(Guid userId, Guid tenantId, string roleName)
    {
        var membership = await _membershipRepository.GetByUserAndTenantAsync(userId, tenantId);
        
        if (membership == null)
        {
            throw new InvalidOperationException(
                $"User {userId} is not a member of tenant {tenantId}");
        }

        membership.RoleName = roleName;
        membership.ModifiedAt = DateTime.UtcNow;
        
        await _membershipRepository.UpdateAsync(membership);
        
        _logger.LogInformation(
            "Assigned role {RoleName} to user {UserId} in tenant {TenantId}",
            roleName, userId, tenantId);
    }

    public async Task RemoveRoleFromUser(Guid userId, Guid tenantId, string roleName)
    {
        var membership = await _membershipRepository.GetByUserAndTenantAsync(userId, tenantId);
        
        if (membership == null || membership.RoleName != roleName)
        {
            return; // Nothing to remove
        }

        // Reset to default "Member" role
        membership.RoleName = "Member";
        membership.ModifiedAt = DateTime.UtcNow;
        
        await _membershipRepository.UpdateAsync(membership);
        
        _logger.LogInformation(
            "Removed role {RoleName} from user {UserId} in tenant {TenantId}",
            roleName, userId, tenantId);
    }
}

