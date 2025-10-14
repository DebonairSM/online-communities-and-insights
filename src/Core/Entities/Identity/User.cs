using OnlineCommunities.Core.Entities.Common;
using OnlineCommunities.Core.Enums;

namespace OnlineCommunities.Core.Entities.Identity;

/// <summary>
/// Represents a user in the system.
/// Supports multiple authentication methods: email/password, social login, and enterprise SSO.
/// Design is future-proof to support all authentication strategies without refactoring.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The authentication method used by this user.
    /// Determines which set of authentication fields are populated.
    /// </summary>
    public AuthenticationMethod AuthMethod { get; set; }

    // ========================================================================
    // Phase 1: Email/Password Authentication (Future)
    // ========================================================================
    
    /// <summary>
    /// Hashed password using bcrypt.
    /// Only populated when AuthMethod = EmailPassword.
    /// </summary>
    public string? PasswordHash { get; set; }

    // ========================================================================
    // Phase 2: Social Login / OAuth Authentication (Current Implementation)
    // ========================================================================
    
    /// <summary>
    /// The OAuth provider used for authentication.
    /// Values: "Google", "GitHub", "Microsoft"
    /// Only populated when AuthMethod = Google/GitHub/MicrosoftPersonal.
    /// </summary>
    public string? ExternalLoginProvider { get; set; }

    /// <summary>
    /// The user's unique identifier at the external OAuth provider.
    /// This is the 'sub' claim from the OAuth provider's token.
    /// Combined with ExternalLoginProvider, this uniquely identifies the user.
    /// </summary>
    public string? ExternalUserId { get; set; }

    // ========================================================================
    // Phase 3: Enterprise SSO / Entra ID Authentication (Future)
    // ========================================================================
    
    /// <summary>
    /// The Azure AD tenant ID that the user belongs to.
    /// Only populated when AuthMethod = EntraId.
    /// This identifies which organization's Entra ID the user comes from.
    /// </summary>
    public string? EntraTenantId { get; set; }

    /// <summary>
    /// The subject identifier from Entra ID (Microsoft Entra ID 'sub' claim).
    /// Only populated when AuthMethod = EntraId.
    /// Combined with EntraTenantId, this uniquely identifies the enterprise user.
    /// </summary>
    public string? EntraIdSubject { get; set; }

    // ========================================================================
    // Application Data
    // ========================================================================

    /// <summary>
    /// Navigation property: Tenant memberships for this user.
    /// A user can belong to multiple tenants with different roles in each.
    /// </summary>
    public ICollection<TenantMembership> TenantMemberships { get; set; } = new List<TenantMembership>();
}

