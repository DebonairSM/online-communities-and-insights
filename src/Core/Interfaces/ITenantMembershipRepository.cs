using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Repository interface for tenant membership and role management.
/// This is where we check user roles (NOT Entra ID!).
/// </summary>
public interface ITenantMembershipRepository : IRepository<TenantMembership>
{
    /// <summary>
    /// Get a user's membership in a specific tenant.
    /// </summary>
    Task<TenantMembership?> GetByUserAndTenantAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Get all memberships for a user.
    /// </summary>
    Task<IEnumerable<TenantMembership>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get all memberships for a tenant.
    /// </summary>
    Task<IEnumerable<TenantMembership>> GetByTenantIdAsync(Guid tenantId);

    /// <summary>
    /// Check if a user has a specific role in a tenant.
    /// THIS IS CALLED BY THE AUTHORIZATION HANDLER!
    /// </summary>
    Task<bool> UserHasRoleInTenantAsync(Guid userId, Guid tenantId, string roleName);

    /// <summary>
    /// Get all role names for a user in a tenant.
    /// </summary>
    Task<List<string>> GetUserRolesInTenantAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Get the user's primary tenant membership (first active membership).
    /// Used for token enrichment to determine primary tenant context.
    /// </summary>
    Task<TenantMembership?> GetPrimaryForUserAsync(Guid userId);
}

