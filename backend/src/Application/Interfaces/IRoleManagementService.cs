namespace OnlineCommunities.Application.Interfaces;

/// <summary>
/// Service interface for managing user roles within tenants.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Checks if a user has a specific role within a tenant.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="tenantId">The tenant ID to check within</param>
    /// <param name="roleName">The name of the role (e.g., "Admin", "Moderator", "Member")</param>
    /// <returns>True if the user has the role in the tenant, false otherwise</returns>
    Task<bool> UserHasRoleInTenant(Guid userId, Guid tenantId, string roleName);

    /// <summary>
    /// Gets all roles for a user within a specific tenant.
    /// </summary>
    Task<List<string>> GetUserRolesInTenant(Guid userId, Guid tenantId);

    /// <summary>
    /// Assigns a role to a user within a tenant.
    /// </summary>
    Task AssignRoleToUser(Guid userId, Guid tenantId, string roleName);

    /// <summary>
    /// Removes a role from a user within a tenant.
    /// </summary>
    Task RemoveRoleFromUser(Guid userId, Guid tenantId, string roleName);
}

