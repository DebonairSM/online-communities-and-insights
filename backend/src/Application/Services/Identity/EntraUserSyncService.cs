using Microsoft.Extensions.Logging;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Core.Interfaces;
using System.Security.Claims;

namespace OnlineCommunities.Application.Services.Identity;

/// <summary>
/// Service for synchronizing users between Microsoft Entra External ID and local database.
/// Handles JIT provisioning and bidirectional sync of user data and custom attributes.
/// </summary>
public class EntraUserSyncService : IEntraUserSyncService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantMembershipRepository _tenantMembershipRepository;
    private readonly ILogger<EntraUserSyncService> _logger;

    public EntraUserSyncService(
        IUserRepository userRepository,
        ITenantMembershipRepository tenantMembershipRepository,
        ILogger<EntraUserSyncService> logger)
    {
        _userRepository = userRepository;
        _tenantMembershipRepository = tenantMembershipRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a user from Entra External ID token claims.
    /// Used for JIT provisioning when user authenticates via Entra External ID.
    /// </summary>
    /// <param name="principal">Claims principal from validated Entra token</param>
    /// <returns>User entity</returns>
    public async Task<User> GetOrCreateUserFromEntraToken(ClaimsPrincipal principal)
    {
        var entraOid = principal.FindFirst("oid")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var givenName = principal.FindFirst("given_name")?.Value;
        var familyName = principal.FindFirst("family_name")?.Value;

        if (string.IsNullOrEmpty(entraOid))
        {
            throw new ArgumentException("Entra OID claim is required", nameof(principal));
        }

        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException("Email claim is required", nameof(principal));
        }

        // Check if user exists by Entra OID
        var user = await _userRepository.GetByEntraOidAsync(entraOid);

        if (user == null)
        {
            // JIT provision from Entra token
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = givenName ?? string.Empty,
                LastName = familyName ?? string.Empty,
                AuthMethod = AuthenticationMethod.EntraExternalId,
                EntraIdSubject = entraOid,
                EmailVerified = true, // Trust Entra External ID
                IsActive = true
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation(
                "JIT provisioned user {UserId} from Entra External ID for email {Email}",
                user.Id, email);
        }
        else
        {
            // Update user profile if needed
            var profileUpdated = false;

            if (!string.IsNullOrEmpty(givenName) && user.FirstName != givenName)
            {
                user.FirstName = givenName;
                profileUpdated = true;
            }

            if (!string.IsNullOrEmpty(familyName) && user.LastName != familyName)
            {
                user.LastName = familyName;
                profileUpdated = true;
            }

            if (!string.IsNullOrEmpty(email) && user.Email != email)
            {
                user.Email = email;
                profileUpdated = true;
            }

            if (profileUpdated)
            {
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation(
                    "Updated user profile {UserId} from Entra External ID claims",
                    user.Id);
            }
        }

        return user;
    }

    /// <summary>
    /// Maps Entra External ID OID to local user ID.
    /// Used by ClaimsPrincipalExtensions.GetUserId() to resolve user ID from Entra token.
    /// </summary>
    /// <param name="entraOid">Entra External ID object identifier</param>
    /// <returns>Local user ID if found, null otherwise</returns>
    public async Task<Guid?> GetUserIdByEntraOid(string entraOid)
    {
        if (string.IsNullOrEmpty(entraOid))
        {
            return null;
        }

        var user = await _userRepository.GetByEntraOidAsync(entraOid);
        return user?.Id;
    }

    /// <summary>
    /// Updates custom attributes in Entra External ID when tenant membership changes.
    /// This pushes tenant/role changes from the local database back to Entra.
    /// </summary>
    /// <param name="userId">User ID to update custom attributes for</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task UpdateEntraCustomAttributes(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.EntraIdSubject))
            {
                _logger.LogWarning("Cannot update Entra attributes: User {UserId} not found or has no EntraIdSubject", userId);
                return;
            }

            var membership = await _tenantMembershipRepository.GetPrimaryForUserAsync(userId);
            if (membership == null)
            {
                _logger.LogWarning("Cannot update Entra attributes: No primary tenant membership for user {UserId}", userId);
                return;
            }

            // Note: This would typically use Microsoft Graph SDK to update user attributes
            // For now, we'll log what would be updated
            _logger.LogInformation(
                "Would update Entra user {EntraOid} with TenantId={TenantId}, Role={Role}",
                user.EntraIdSubject, membership.TenantId, membership.RoleName);

            // TODO: Implement actual Graph SDK call:
            // var graphClient = new GraphServiceClient(...);
            // await graphClient.Users[user.EntraIdSubject]
            //     .Request()
            //     .UpdateAsync(new User {
            //         AdditionalData = new Dictionary<string, object> {
            //             { "extension_TenantId", membership.TenantId.ToString() },
            //             { "extension_Roles", new[] { membership.RoleName } }
            //         }
            //     });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Entra custom attributes for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Updates custom attributes in Entra External ID when user's roles change in a tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task UpdateEntraCustomAttributesForTenant(Guid userId, Guid tenantId)
    {
        try
        {
            var membership = await _tenantMembershipRepository.GetByUserAndTenantAsync(userId, tenantId);
            if (membership == null)
            {
                _logger.LogWarning("Cannot update Entra attributes: No tenant membership for user {UserId} in tenant {TenantId}", userId, tenantId);
                return;
            }

            // Update the custom attributes for the specific tenant
            await UpdateEntraCustomAttributes(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Entra custom attributes for user {UserId} in tenant {TenantId}", userId, tenantId);
            throw;
        }
    }
}
