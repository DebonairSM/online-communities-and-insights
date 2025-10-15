# ADR-007: Self-Issued JWT Tokens

**Date**: 2025-01-15
**Status**: Accepted
**Deciders**: Platform Developer
**Technical Story**: Unified token format across authentication methods

---

## Context

For Phase 1 (Email/Password) and Phase 2 (Social Login), we need to decide whether to:
1. Use tokens issued by authentication providers (Google, Microsoft, GitHub)
2. Generate our own JWT tokens after validating provider credentials

## Decision

**For all authentication methods, we will ALWAYS generate our own JWT tokens.**

This applies regardless of which authentication provider is used.

## Rationale

### 1. Unified Token Format
All authentication methods produce the same JWT structure:

```json
{
  "iss": "OnlineCommunitiesAPI",
  "aud": "OnlineCommunitiesUsers",
  "sub": "user-id-guid",
  "email": "user@example.com",
  "jti": "unique-token-id",
  "user_id": "user-id-guid",
  "auth_method": "Google" | "GitHub" | "Microsoft" | "EmailPassword",
  "tenant_id": "tenant-guid",
  "role": ["Admin", "Moderator", "Member"],
  "permissions": ["post.create", "comment.delete"],
  "exp": 1234567890
}
```

### 2. Application-Specific Claims
We control claims that external providers don't know about:
- `tenant_id` - Which tenant user belongs to
- `role` - User's roles in our multi-tenant system
- `permissions` - Granular permissions from our database
- `auth_method` - Track which authentication method (for analytics)

### 3. Token Lifecycle Control
- We decide token expiry (15 minutes for access tokens)
- We implement refresh logic consistently
- Same refresh endpoint for all auth methods
- We control revocation (logout, security events)

### 4. Provider Independence
All authentication methods use the same code path:
```csharp
// All methods call the same token generation
var jwt = _externalAuthService.GenerateJwtToken(user);

// Email/Password
var user = await VerifyPassword(email, password);
return GenerateJwtToken(user);

// OAuth providers
var user = await HandleOAuthCallback(code, provider);
return GenerateJwtToken(user);
```

### 5. Simplified Validation
One validation path for all tokens:
- Single JWT validation middleware
- One set of signing keys
- Consistent claims structure
- No provider-specific validation logic

## Implementation Status

### Phase 2 (Social Login) - ✅ Implemented Correctly
- `ExternalAuthService.GenerateJwtToken()` creates our JWT
- `AuthController.OAuthCallback()` returns our JWT after OAuth
- `Program.cs` validates our JWT tokens
- Uses OAuth packages only for authentication, not for tokens
- No Microsoft.Identity.Web package (correct for our approach)

### Phase 1 (Email/Password) - 🚧 Future Implementation
When implemented, will use the same `GenerateJwtToken()` method:

```csharp
public async Task<(User user, string jwt)> Login(string email, string password)
{
    var user = await _userRepository.GetByEmailAsync(email);
    if (!BCrypt.Verify(password, user.PasswordHash))
        throw new UnauthorizedException();
    
    // REUSE THE SAME JWT GENERATION!
    var jwt = _externalAuthService.GenerateJwtToken(user);
    return (user, jwt);
}
```

## Consequences

### Positive
- Frontend doesn't care which auth method was used
- Authorization logic identical for all methods
- One token validation path in API
- Consistent user experience
- Easy to add new authentication providers

### Negative
- More complex than using provider tokens directly
- Must implement token refresh logic
- Additional server-side token generation

### Neutral
- Need to map external provider claims to internal user model
- Must maintain user identity linking across providers

## Validation

**Success Criteria**:
- ✅ Single JWT validation middleware handles all auth methods
- ✅ Same claims structure regardless of authentication provider
- ✅ Refresh token endpoint works for all auth methods
- [ ] New auth methods integrate without frontend changes

## References
- [Authentication Strategy](./006-authentication-strategy.md)
- [JWT RFC 7519](https://tools.ietf.org/html/rfc7519)
- [OAuth 2.0 RFC 6749](https://tools.ietf.org/html/rfc6749)
- Related: `contexts/security-model.md` - Authentication implementation details
