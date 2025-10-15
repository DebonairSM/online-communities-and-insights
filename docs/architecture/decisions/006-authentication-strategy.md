# ADR-006: Authentication Strategy

**Date**: 2025-01-15
**Status**: Accepted  
**Deciders**: Platform Developer
**Technical Story**: Multi-phase authentication implementation

---

## Context

The Insight Community Platform requires a flexible authentication system that can evolve from simple social login to enterprise SSO. We need to support multiple authentication methods while maintaining a consistent user experience and security posture.

## Decision

**We will implement authentication in phases with self-issued JWT tokens for all authentication methods.**

### Phase 1: Email/Password (Future - Not Implemented)
- Traditional email/password registration and login
- bcrypt password hashing (work factor 12) 
- Email verification workflow
- Password reset via secure tokens
- MFA support (TOTP and SMS)

### Phase 2: Social Login (Current - ✅ Implemented)
- OAuth 2.0 with Google, GitHub, Microsoft personal accounts
- Just-in-time (JIT) user provisioning
- Self-issued JWT tokens (not provider tokens)
- Unified token format regardless of OAuth provider

### Phase 3: Enterprise SSO (Future)
- Microsoft Entra ID (Azure AD) multi-tenant
- SAML 2.0 support for other identity providers
- JIT provisioning with attribute mapping
- Same JWT token format as other phases

## Implementation Details

### Self-Issued JWT Structure
All authentication methods produce the same JWT:

```json
{
  "iss": "OnlineCommunitiesAPI",
  "aud": "OnlineCommunitiesUsers", 
  "sub": "user-id-guid",
  "email": "user@example.com",
  "user_id": "user-id-guid",
  "auth_method": "Google" | "GitHub" | "Microsoft" | "EmailPassword" | "EntraID",
  "tenant_id": "tenant-guid",
  "role": ["Admin", "Moderator", "Member"],
  "permissions": ["post.create", "comment.delete"],
  "exp": 1234567890
}
```

### OAuth 2.0 Flow (Phase 2)
1. User clicks "Sign in with [Provider]"
2. Redirect to provider's authorization endpoint  
3. User authenticates with provider
4. Provider redirects back with authorization code
5. Our API exchanges code for user info (not tokens)
6. Create/update local user account
7. Issue our own JWT token
8. Return JWT to frontend

### Key Benefits
- **Consistent Authorization**: Same token validation for all auth methods
- **Application-Specific Claims**: Include our tenant/role data
- **Provider Independence**: Can switch OAuth providers without breaking clients
- **Future-Proof**: Easy to add new auth methods using same token format

## Consequences

### Positive
- Unified authorization logic across all authentication methods
- Full control over token lifecycle and claims
- Easy to add new authentication providers
- Consistent user experience regardless of auth method

### Negative  
- Must implement token generation and refresh logic
- Cannot use provider tokens directly for API access
- Additional complexity compared to accepting provider tokens

### Neutral
- Need to maintain user mapping between providers and local accounts
- Must handle token refresh uniformly across all auth methods

## References
- [Authentication Decision](./007-authentication-decision.md)
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/azure/active-directory/)
- Related: `contexts/security-model.md` - Detailed authentication flows
