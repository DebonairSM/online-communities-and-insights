namespace OnlineCommunities.Core.Enums;

/// <summary>
/// Defines the authentication method used by a user.
/// Supports multiple authentication strategies for flexibility.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Phase 1: Traditional email and password authentication.
    /// User registers with email/password, we hash and store password.
    /// </summary>
    EmailPassword = 1,
    
    /// <summary>
    /// Phase 2: OAuth login with Google.
    /// User authenticates with their Google account (Gmail, Google Workspace personal).
    /// </summary>
    Google = 2,
    
    /// <summary>
    /// Phase 2: OAuth login with GitHub.
    /// User authenticates with their GitHub account.
    /// </summary>
    GitHub = 3,
    
    /// <summary>
    /// Phase 2: OAuth login with Microsoft Personal Account.
    /// User authenticates with Outlook.com, Hotmail.com, or Live.com accounts.
    /// NOT the same as Entra ID (work accounts).
    /// </summary>
    MicrosoftPersonal = 4,
    
    /// <summary>
    /// Phase 3: Enterprise SSO with Microsoft Entra ID (Azure AD).
    /// User authenticates with their organization's work account.
    /// Supports multi-tenant Azure AD scenarios.
    /// </summary>
    EntraId = 5
}

