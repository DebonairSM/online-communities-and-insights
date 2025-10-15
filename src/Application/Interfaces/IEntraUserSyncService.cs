using OnlineCommunities.Core.Entities.Identity;
using System.Security.Claims;

namespace OnlineCommunities.Application.Interfaces;

/// <summary>
/// Service for synchronizing users between Microsoft Entra External ID and local database.
/// Handles JIT provisioning and bidirectional sync of user data and custom attributes.
/// </summary>
public interface IEntraUserSyncService
{
    /// <summary>
    /// Gets or creates a user from Entra External ID token claims.
    /// Used for JIT provisioning when user authenticates via Entra External ID.
    /// </summary>
    /// <param name="principal">Claims principal from validated Entra token</param>
    /// <returns>User entity</returns>
    Task<User> GetOrCreateUserFromEntraToken(ClaimsPrincipal principal);

    /// <summary>
    /// Maps Entra External ID OID to local user ID.
    /// Used by ClaimsPrincipalExtensions.GetUserId() to resolve user ID from Entra token.
    /// </summary>
    /// <param name="entraOid">Entra External ID object identifier</param>
    /// <returns>Local user ID if found, null otherwise</returns>
    Task<Guid?> GetUserIdByEntraOid(string entraOid);

    /// <summary>
    /// Updates custom attributes in Entra External ID when tenant membership changes.
    /// This pushes tenant/role changes from the local database back to Entra.
    /// </summary>
    /// <param name="userId">User ID to update custom attributes for</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateEntraCustomAttributes(Guid userId);

    /// <summary>
    /// Updates custom attributes in Entra External ID when user's roles change in a tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateEntraCustomAttributesForTenant(Guid userId, Guid tenantId);
}
