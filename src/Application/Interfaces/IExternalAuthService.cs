using OnlineCommunities.Core.Entities.Identity;
using System.Security.Claims;

namespace OnlineCommunities.Application.Interfaces;

/// <summary>
/// Service for handling external OAuth authentication (social login).
/// Supports Google, GitHub, Microsoft Personal Accounts, and future providers.
/// </summary>
public interface IExternalAuthService
{
    /// <summary>
    /// Handles external OAuth login, creating or updating user as needed.
    /// Returns the user entity and a JWT token for API access.
    /// </summary>
    /// <param name="provider">OAuth provider name ("Google", "GitHub", "Microsoft")</param>
    /// <param name="externalUserId">User's unique ID at the OAuth provider</param>
    /// <param name="email">User's email from OAuth provider</param>
    /// <param name="firstName">User's first name (optional)</param>
    /// <param name="lastName">User's last name (optional)</param>
    /// <returns>Tuple of (User entity, JWT token)</returns>
    Task<(User user, string jwtToken)> HandleExternalLoginAsync(
        string provider,
        string externalUserId,
        string email,
        string? firstName,
        string? lastName);

    /// <summary>
    /// Generates a JWT token for the given user.
    /// Token is used for subsequent API requests.
    /// </summary>
    /// <param name="user">User entity to generate token for</param>
    /// <returns>JWT token string</returns>
    string GenerateJwtToken(User user);

    /// <summary>
    /// Validates a JWT token and returns the user ID if valid.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ID if valid, null if invalid</returns>
    Task<Guid?> ValidateTokenAsync(string token);
}

