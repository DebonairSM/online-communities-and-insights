using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Repository interface for User-specific queries.
/// Extends the generic repository with user-specific methods.
/// Supports multiple authentication methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Find a user by their email address.
    /// Used across all authentication methods.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Find a user by external OAuth login (Phase 2: Social Login).
    /// Looks up user by the combination of provider and external user ID.
    /// Example: ("Google", "google-user-id-123")
    /// </summary>
    Task<User?> GetByExternalLoginAsync(string provider, string externalUserId);

    /// <summary>
    /// Find a user by their Entra ID subject identifier (Phase 3: Enterprise SSO).
    /// Called during JIT provisioning after Entra ID login.
    /// </summary>
    Task<User?> GetByEntraIdSubjectAsync(string entraIdSubject);

    /// <summary>
    /// Find a user by their Entra External ID object identifier (OID).
    /// Used for mapping Entra External ID users to local user records.
    /// </summary>
    Task<User?> GetByEntraOidAsync(string entraOid);

    /// <summary>
    /// Get all tenants that a user belongs to.
    /// </summary>
    Task<IEnumerable<Guid>> GetUserTenantIdsAsync(Guid userId);

    /// <summary>
    /// Check if a user is a member of a specific tenant.
    /// </summary>
    Task<bool> IsMemberOfTenantAsync(Guid userId, Guid tenantId);
}

