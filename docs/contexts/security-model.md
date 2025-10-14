# Security Model

## Authentication

### Local Authentication (Email + Password)

**Registration Flow**:
1. User submits email and password via registration form
2. Validate email format and password complexity
3. Check for existing account with same email
4. Hash password using bcrypt (work factor 12)
5. Create user record with `EmailVerified = false`
6. Generate email verification token (JWT with 24-hour expiry)
7. Send verification email with token link
8. User clicks link, token validated, account activated

**Password Requirements**:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character
- Not in common password breach lists (checked against Have I Been Pwned API)

**Password Hashing**:
```csharp
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}

public bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

**Login Flow**:
1. User submits email and password
2. Query user by email
3. Verify password hash
4. Check if email is verified
5. Check if account is active (not suspended or deleted)
6. Generate JWT access token (15-minute expiry)
7. Generate refresh token (7-day expiry)
8. Store refresh token in httpOnly cookie or return in response
9. Return access token and user profile

**Password Reset**:
1. User requests reset via email
2. Generate password reset token (random 32-byte string)
3. Store token with expiration (1 hour) in database
4. Send email with reset link
5. User clicks link, submits new password
6. Validate token and expiration
7. Hash new password and update user record
8. Invalidate all existing sessions and tokens
9. Send confirmation email

### Single Sign-On (OAuth 2.0 / OIDC)

**Supported Providers** (per-tenant configuration):
- Azure Active Directory / Entra ID
- Okta
- Auth0
- Google Workspace
- SAML 2.0 (generic)

**Authorization Code Flow with PKCE**:
```csharp
// 1. Generate PKCE code verifier and challenge
var codeVerifier = GenerateCodeVerifier();
var codeChallenge = GenerateCodeChallenge(codeVerifier);

// 2. Redirect to authorization endpoint
var authUrl = $"{identityProvider.AuthorizationEndpoint}?" +
    $"client_id={clientId}&" +
    $"redirect_uri={redirectUri}&" +
    $"response_type=code&" +
    $"scope=openid profile email&" +
    $"state={state}&" +
    $"code_challenge={codeChallenge}&" +
    $"code_challenge_method=S256";

// 3. User authenticates with identity provider

// 4. Identity provider redirects back with authorization code

// 5. Exchange code for tokens
var tokenRequest = new HttpRequestMessage(HttpMethod.Post, identityProvider.TokenEndpoint);
tokenRequest.Content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("grant_type", "authorization_code"),
    new KeyValuePair<string, string>("code", authorizationCode),
    new KeyValuePair<string, string>("redirect_uri", redirectUri),
    new KeyValuePair<string, string>("client_id", clientId),
    new KeyValuePair<string, string>("code_verifier", codeVerifier)
});

var tokenResponse = await _httpClient.SendAsync(tokenRequest);
var tokens = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

// 6. Validate ID token
var principal = ValidateIdToken(tokens.IdToken);

// 7. Create or update local user account (JIT provisioning)
var user = await CreateOrUpdateUser(principal);

// 8. Generate internal JWT tokens
var accessToken = GenerateAccessToken(user);
var refreshToken = GenerateRefreshToken(user);
```

**Just-In-Time Provisioning**:
```csharp
public async Task<User> CreateOrUpdateUser(ClaimsPrincipal principal)
{
    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
    var user = await _userRepository.GetByEmail(email);
    
    if (user == null)
    {
        user = new User
        {
            Email = email,
            FirstName = principal.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = principal.FindFirst(ClaimTypes.Surname)?.Value,
            EmailVerified = true, // Trust SSO provider
            AuthProvider = "AzureAD"
        };
        await _userRepository.Create(user);
    }
    else
    {
        // Update profile from SSO attributes
        user.FirstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
        user.LastName = principal.FindFirst(ClaimTypes.Surname)?.Value;
        await _userRepository.Update(user);
    }
    
    return user;
}
```

### Multi-Factor Authentication (MFA)

**TOTP (Time-Based One-Time Password)**:
```csharp
public async Task<string> EnableMfa(Guid userId)
{
    var user = await _userRepository.GetById(userId);
    var secret = GenerateTotpSecret();
    
    user.MfaSecret = _encryptionService.Encrypt(secret);
    user.MfaEnabled = false; // Enabled after first successful verification
    await _userRepository.Update(user);
    
    // Generate QR code for authenticator apps
    var qrCodeUrl = $"otpauth://totp/{user.Email}?secret={secret}&issuer=OnlineCommunities";
    return qrCodeUrl;
}

public async Task<bool> VerifyMfaCode(Guid userId, string code)
{
    var user = await _userRepository.GetById(userId);
    var secret = _encryptionService.Decrypt(user.MfaSecret);
    
    var totp = new Totp(Base32Encoding.ToBytes(secret));
    var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    
    if (isValid && !user.MfaEnabled)
    {
        user.MfaEnabled = true;
        await _userRepository.Update(user);
    }
    
    return isValid;
}
```

**SMS-Based MFA** (via Twilio or Azure Communication Services):
```csharp
public async Task SendMfaCode(Guid userId)
{
    var user = await _userRepository.GetById(userId);
    var code = GenerateRandomCode(6); // 6-digit code
    
    // Store code with expiration
    await _cache.SetAsync($"mfa:{userId}", code, TimeSpan.FromMinutes(5));
    
    // Send SMS
    await _smsService.SendAsync(user.PhoneNumber, $"Your verification code is: {code}");
}
```

## Research Consent Management

### Study-Level Consent

**Explicit Consent Requirement**: Before participating in any research activity, participants must provide informed consent specific to that study.

**Consent Capture Flow**:
```csharp
public class ConsentService
{
    public async Task<ConsentRecord> CaptureConsent(Guid userId, Guid studyId, ConsentFormData consentData)
    {
        var consentRecord = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StudyId = studyId,
            ConsentFormVersion = consentData.Version,
            ConsentText = consentData.FullText,
            ConsentedAt = DateTime.UtcNow,
            IpAddress = consentData.IpAddress,
            UserAgent = consentData.UserAgent,
            ElectronicSignature = consentData.Signature,
            IsMinor = consentData.ParticipantAge < 18,
            ParentalConsentId = consentData.ParentalConsentId, // If minor
            ConsentType = ConsentType.Informed, // Informed, Broad, Opt-In
            DataUsageAgreement = new DataUsageAgreement
            {
                AllowVideoRecording = consentData.AllowVideo,
                AllowAudioRecording = consentData.AllowAudio,
                AllowPhotoCapture = consentData.AllowPhotos,
                AllowDataSharing = consentData.AllowSharing,
                AllowIdentifiableQuotes = consentData.AllowQuotes,
                AllowMediaInReports = consentData.AllowMediaInReports
            }
        };
        
        await _consentRepository.Create(consentRecord);
        
        // Publish event for audit trail
        await _eventBus.Publish(new ConsentGranted
        {
            UserId = userId,
            StudyId = studyId,
            ConsentId = consentRecord.Id,
            Timestamp = DateTime.UtcNow
        });
        
        return consentRecord;
    }
}
```

**Consent Form Requirements**:
- Plain language explanation of study purpose
- What data will be collected (surveys, video, photos, etc.)
- How long data will be retained
- How data will be used (internal analysis, client reports, public presentations)
- Participant rights (withdraw, data access, deletion)
- Contact information for questions
- IRB approval number (if applicable)

**Minor Protection**:
```csharp
public async Task<bool> RequiresParentalConsent(Guid userId)
{
    var user = await _userRepository.GetById(userId);
    var age = CalculateAge(user.DateOfBirth);
    
    return age < 18; // Varies by jurisdiction
}

public async Task RequestParentalConsent(Guid minorUserId, string parentEmail)
{
    var consentLink = GenerateSecureConsentLink(minorUserId);
    
    await _emailService.SendParentalConsentRequest(
        parentEmail,
        minorUserId,
        consentLink,
        expiresIn: TimeSpan.FromDays(7)
    );
}
```

### GDPR Article 9 - Sensitive Data

**Special Categories of Data** (require explicit consent and additional safeguards):
- Health data (medical conditions, disabilities)
- Biometric data (facial recognition, voice prints)
- Genetic data
- Political opinions
- Religious beliefs
- Sexual orientation

**Enhanced Protection**:
```csharp
public class SensitiveDataService
{
    public async Task<bool> CanCollectSensitiveData(Guid studyId, SensitiveDataType dataType)
    {
        var study = await _studyRepository.GetById(studyId);
        
        // Check if study has explicit sensitive data consent
        if (!study.ConsentForm.IncludesSensitiveData)
        {
            return false;
        }
        
        // Verify specific data type is approved
        if (!study.ApprovedSensitiveDataTypes.Contains(dataType))
        {
            return false;
        }
        
        // Check if legal basis exists (scientific research, public interest)
        if (study.LegalBasis == null)
        {
            return false;
        }
        
        return true;
    }
    
    public async Task MarkResponseAsSensitive(Guid responseId, SensitiveDataType dataType)
    {
        await _responseRepository.UpdateSensitiveDataFlag(responseId, dataType);
        
        // Separate storage and access controls applied automatically
        await _accessControlService.ApplyEnhancedProtection(responseId);
        
        // Audit log
        await _auditService.Log(new AuditLog
        {
            Action = "sensitive_data_marked",
            EntityType = "Response",
            EntityId = responseId,
            Metadata = new { DataType = dataType.ToString() }
        });
    }
}
```

**Data Processing Agreement**:
- Separate storage for sensitive data
- Encryption at rest with tenant-specific keys
- Restricted access (only authorized researchers)
- Automatic deletion after retention period
- No cross-border transfer without explicit consent

### Consent Withdrawal

**Right to Withdraw**:
```csharp
public async Task WithdrawConsent(Guid userId, Guid studyId, WithdrawalPreferences preferences)
{
    var consentRecord = await _consentRepository.GetByUserAndStudy(userId, studyId);
    
    consentRecord.WithdrawnAt = DateTime.UtcNow;
    consentRecord.WithdrawalReason = preferences.Reason;
    await _consentRepository.Update(consentRecord);
    
    // Handle data based on participant preference
    switch (preferences.DataHandling)
    {
        case DataHandlingPreference.DeleteAll:
            await DeleteAllUserData(userId, studyId);
            break;
            
        case DataHandlingPreference.AnonymizeAndRetain:
            await AnonymizeUserData(userId, studyId);
            break;
            
        case DataHandlingPreference.RetainForResearch:
            // Data retained but no further contact
            await MarkAsNoContact(userId, studyId);
            break;
    }
    
    // Notify research team
    await _notificationService.NotifyResearchTeam(
        studyId,
        $"Participant {userId} withdrew consent with {preferences.DataHandling} option"
    );
}
```

## Authorization

### Role-Based Access Control (RBAC)

**System Roles**:
- `PlatformAdmin`: Full system access across all tenants
- `TenantAdmin`: Full access within tenant
- `CommunityAdmin`: Full access within specific community
- `Moderator`: Content management and moderation
- `Member`: Standard user access
- `Guest`: Read-only access (if enabled)

**Role Hierarchy**:
```
PlatformAdmin
  └─ TenantAdmin
      └─ CommunityAdmin
          └─ Moderator
              └─ Member
                  └─ Guest
```

**Implementation**:
```csharp
[Authorize(Roles = "Moderator,Admin")]
public async Task<IActionResult> DeletePost(Guid postId)
{
    var post = await _postRepository.GetById(postId);
    
    // Additional check: moderators can only delete within their communities
    if (User.IsInRole("Moderator") && !User.IsInRole("Admin"))
    {
        var communityId = post.CommunityId;
        var isModerator = await _membershipRepository.IsModerator(User.UserId(), communityId);
        if (!isModerator)
        {
            return Forbid();
        }
    }
    
    await _postRepository.Delete(postId);
    return NoContent();
}
```

### Attribute-Based Access Control (ABAC)

**Resource-Level Permissions**:
```csharp
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourcePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement)
    {
        var user = context.User;
        var resource = context.Resource as Post;
        
        if (resource == null)
        {
            context.Fail();
            return;
        }
        
        // Check if user is the author
        if (requirement.Permission == "edit" || requirement.Permission == "delete")
        {
            if (resource.AuthorId == user.UserId())
            {
                context.Succeed(requirement);
                return;
            }
        }
        
        // Check if user has moderator role in the community
        var isModerator = await _membershipRepository.IsModerator(
            user.UserId(), 
            resource.CommunityId
        );
        
        if (isModerator && requirement.Permission == "delete")
        {
            context.Succeed(requirement);
            return;
        }
        
        context.Fail();
    }
}
```

**Usage**:
```csharp
[Authorize]
public async Task<IActionResult> EditPost(Guid postId, UpdatePostDto dto)
{
    var post = await _postRepository.GetById(postId);
    
    var authResult = await _authorizationService.AuthorizeAsync(
        User, 
        post, 
        new ResourcePermissionRequirement("edit")
    );
    
    if (!authResult.Succeeded)
    {
        return Forbid();
    }
    
    // Proceed with edit
    post.Content = dto.Content;
    await _postRepository.Update(post);
    return Ok(post);
}
```

### Policy-Based Authorization

**Custom Policies**:
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTenantMembership", policy =>
        policy.Requirements.Add(new TenantMembershipRequirement()));
    
    options.AddPolicy("RequireEmailVerification", policy =>
        policy.RequireClaim("email_verified", "true"));
    
    options.AddPolicy("RequireActiveSubscription", policy =>
        policy.Requirements.Add(new ActiveSubscriptionRequirement()));
    
    options.AddPolicy("CanManageTenant", policy =>
        policy.RequireRole("TenantAdmin", "PlatformAdmin"));
});
```

**Policy Handlers**:
```csharp
public class TenantMembershipRequirement : IAuthorizationRequirement { }

public class TenantMembershipHandler : AuthorizationHandler<TenantMembershipRequirement>
{
    private readonly IMembershipRepository _membershipRepository;
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMembershipRequirement requirement)
    {
        var tenantId = context.User.TenantId();
        var userId = context.User.UserId();
        
        var isMember = await _membershipRepository.IsMemberOfTenant(userId, tenantId);
        
        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}
```

## JWT Token Structure

### Access Token

**Claims**:
```json
{
  "sub": "user-id-guid",
  "email": "user@example.com",
  "name": "John Doe",
  "tenantId": "tenant-id-guid",
  "role": ["Member", "Moderator"],
  "permissions": [
    "community.read",
    "post.create",
    "post.edit.own",
    "comment.create",
    "survey.respond"
  ],
  "email_verified": "true",
  "iat": 1728654600,
  "exp": 1728655500,
  "iss": "https://api.communities.com",
  "aud": "https://api.communities.com"
}
```

**Generation**:
```csharp
public string GenerateAccessToken(User user, Guid tenantId, List<string> roles)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.Name, user.DisplayName),
        new("tenantId", tenantId.ToString()),
        new("email_verified", user.EmailVerified.ToString().ToLower())
    };
    
    // Add roles
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    
    // Add permissions based on roles
    var permissions = GetPermissionsForRoles(roles);
    foreach (var permission in permissions)
    {
        claims.Add(new Claim("permission", permission));
    }
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: credentials
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Refresh Token

**Structure**: Opaque token (random 64-byte string encoded as Base64)

**Storage**: Database table with user ID, token hash, expiration, revocation status

```csharp
public async Task<string> GenerateRefreshToken(Guid userId)
{
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    
    var refreshToken = new RefreshToken
    {
        UserId = userId,
        TokenHash = Convert.ToBase64String(tokenHash),
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        IsRevoked = false
    };
    
    await _refreshTokenRepository.Create(refreshToken);
    
    return token;
}

public async Task<string> RefreshAccessToken(string refreshToken)
{
    var tokenHash = Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))
    );
    
    var storedToken = await _refreshTokenRepository.GetByTokenHash(tokenHash);
    
    if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
    {
        throw new UnauthorizedException("Invalid or expired refresh token");
    }
    
    var user = await _userRepository.GetById(storedToken.UserId);
    var tenantId = await _membershipRepository.GetPrimaryTenant(user.Id);
    var roles = await _roleRepository.GetUserRoles(user.Id, tenantId);
    
    return GenerateAccessToken(user, tenantId, roles);
}
```

### Token Revocation

**Revoke on Logout**:
```csharp
public async Task RevokeRefreshToken(string refreshToken)
{
    var tokenHash = Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))
    );
    
    var storedToken = await _refreshTokenRepository.GetByTokenHash(tokenHash);
    
    if (storedToken != null)
    {
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.Update(storedToken);
    }
}
```

**Revoke All Sessions** (on password change, security breach):
```csharp
public async Task RevokeAllUserTokens(Guid userId)
{
    await _refreshTokenRepository.RevokeAllForUser(userId);
    
    // Add access token JTI to blacklist (cached in Redis)
    var activeAccessTokens = await GetActiveAccessTokensForUser(userId);
    foreach (var token in activeAccessTokens)
    {
        await _cache.SetAsync($"blacklist:{token.Jti}", "revoked", token.ExpiresAt - DateTime.UtcNow);
    }
}
```

**Check Blacklist Middleware**:
```csharp
public class TokenBlacklistMiddleware
{
    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            
            if (jti != null)
            {
                var isBlacklisted = await cache.GetStringAsync($"blacklist:{jti}");
                
                if (isBlacklisted != null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }
            }
        }
        
        await _next(context);
    }
}
```

## Tenant Isolation

### Tenant Context Middleware

```csharp
public class TenantContextMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = ExtractTenantId(context);
        
        if (tenantId == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Tenant ID is required");
            return;
        }
        
        var tenant = await tenantService.GetTenant(tenantId.Value);
        
        if (tenant == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Tenant not found");
            return;
        }
        
        if (tenant.Status != TenantStatus.Active)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Tenant is not active");
            return;
        }
        
        context.Items["TenantId"] = tenantId.Value;
        context.Items["Tenant"] = tenant;
        
        await _next(context);
    }
    
    private Guid? ExtractTenantId(HttpContext context)
    {
        // Option 1: From subdomain
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.')[0];
        var tenant = await _tenantService.GetBySubdomain(subdomain);
        if (tenant != null) return tenant.Id;
        
        // Option 2: From JWT claim
        var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }
        
        // Option 3: From custom header
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue))
        {
            if (Guid.TryParse(headerValue, out var headerTenantId))
            {
                return headerTenantId;
            }
        }
        
        return null;
    }
}
```

### Database Row-Level Security

**EF Core Global Query Filter**:
```csharp
public class TenantScopedDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Apply tenant filter to all tenant-scoped entities
        builder.Entity<Post>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        builder.Entity<Comment>().HasQueryFilter(c => c.TenantId == CurrentTenantId);
        builder.Entity<Community>().HasQueryFilter(c => c.TenantId == CurrentTenantId);
        builder.Entity<User>().HasQueryFilter(u => u.Memberships.Any(m => m.TenantId == CurrentTenantId));
    }
    
    private Guid CurrentTenantId => 
        (Guid)_httpContextAccessor.HttpContext.Items["TenantId"];
}
```

**SQL Server Row-Level Security** (defense in depth):
```sql
CREATE FUNCTION dbo.fn_TenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS AccessGranted
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);
GO

CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId)
ON dbo.Posts,
ON dbo.Comments,
ON dbo.Communities
WITH (STATE = ON);
GO

-- Set tenant context at connection level
EXEC sp_set_session_context 'TenantId', @TenantId;
```

### Cross-Tenant Access Prevention

**Validation Attribute**:
```csharp
public class ValidateTenantAccessAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var currentTenantId = (Guid)context.HttpContext.Items["TenantId"];
        
        // Check all GUID parameters for tenant ownership
        foreach (var parameter in context.ActionArguments.Values)
        {
            if (parameter is Guid resourceId)
            {
                var resource = await GetResource(resourceId);
                if (resource?.TenantId != currentTenantId)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
        
        await next();
    }
}
```

## Input Validation and Sanitization

### Model Validation

```csharp
public class CreatePostDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; }
    
    [Required]
    [StringLength(50000, MinimumLength = 1)]
    public string Content { get; set; }
    
    [Required]
    public Guid CommunityId { get; set; }
    
    [Url]
    public string? LinkUrl { get; set; }
}
```

### XSS Prevention

**HTML Sanitization** (for rich text content):
```csharp
public string SanitizeHtml(string html)
{
    var sanitizer = new HtmlSanitizer();
    
    // Allow only safe tags
    sanitizer.AllowedTags.Clear();
    sanitizer.AllowedTags.Add("p");
    sanitizer.AllowedTags.Add("br");
    sanitizer.AllowedTags.Add("strong");
    sanitizer.AllowedTags.Add("em");
    sanitizer.AllowedTags.Add("ul");
    sanitizer.AllowedTags.Add("ol");
    sanitizer.AllowedTags.Add("li");
    sanitizer.AllowedTags.Add("a");
    
    // Allow specific attributes
    sanitizer.AllowedAttributes.Add("href");
    sanitizer.AllowedSchemes.Add("https");
    sanitizer.AllowedSchemes.Add("http");
    
    return sanitizer.Sanitize(html);
}
```

**Content Security Policy** (CSP):
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.example.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://api.example.com; " +
        "frame-ancestors 'none';");
    
    await next();
});
```

### SQL Injection Prevention

**Parameterized Queries** (via Entity Framework):
```csharp
// Safe - parameterized
var posts = await _context.Posts
    .Where(p => p.TenantId == tenantId && p.Title.Contains(searchTerm))
    .ToListAsync();

// Unsafe - string concatenation (DO NOT DO THIS)
var query = $"SELECT * FROM Posts WHERE Title LIKE '%{searchTerm}%'";
```

### Rate Limiting

**Per-User Rate Limits**:
```csharp
public class RateLimitMiddleware
{
    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var userId = context.User.UserId();
        var endpoint = context.Request.Path;
        var key = $"ratelimit:{userId}:{endpoint}";
        
        var requestCount = await cache.GetStringAsync(key);
        var count = requestCount == null ? 0 : int.Parse(requestCount);
        
        if (count >= 100) // 100 requests per minute
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        await cache.SetStringAsync(key, (count + 1).ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });
        
        await _next(context);
    }
}
```

## Data Protection

### Encryption at Rest

**Azure SQL Database**: Transparent Data Encryption (TDE) enabled by default

**Blob Storage**: Server-side encryption with Microsoft-managed keys or customer-managed keys in Key Vault

**Sensitive Fields** (additional application-level encryption):
```csharp
public class EncryptionService
{
    private readonly byte[] _key;
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }
    
    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        
        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];
        
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

### Encryption in Transit

**TLS 1.2+ Required**:
```csharp
services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

app.UseHsts();
app.UseHttpsRedirection();
```

**Certificate Pinning** (for mobile apps):
```csharp
// Mobile app configuration
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
{
    var expectedThumbprint = "EXPECTED_CERT_THUMBPRINT";
    return cert.GetCertHashString() == expectedThumbprint;
};

var client = new HttpClient(handler);
```

### Media Privacy & Anonymization

**Separate Storage for Identifiable Media**:
```csharp
public class MediaStorageService
{
    public async Task<string> StoreParticipantMedia(Stream mediaStream, MediaMetadata metadata)
    {
        // Store identifiable media separately from research data
        var containerName = metadata.ContainsIdentifiableContent 
            ? $"identifiable-media-{metadata.TenantId}"
            : $"research-media-{metadata.TenantId}";
        
        var blobClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await blobClient.CreateIfNotExistsAsync(PublicAccessType.None);
        
        var blobName = $"{metadata.StudyId}/{metadata.ParticipantId}/{metadata.FileName}";
        var blob = blobClient.GetBlobClient(blobName);
        
        // Add metadata tags for access control
        var tags = new Dictionary<string, string>
        {
            { "StudyId", metadata.StudyId.ToString() },
            { "ParticipantId", metadata.ParticipantId.ToString() },
            { "ConsentId", metadata.ConsentId.ToString() },
            { "ContainsIdentifiableContent", metadata.ContainsIdentifiableContent.ToString() },
            { "RequiresAnonymization", metadata.RequiresAnonymization.ToString() }
        };
        
        await blob.UploadAsync(mediaStream);
        await blob.SetTagsAsync(tags);
        
        return blob.Uri.ToString();
    }
}
```

**Automatic Face Blurring**:
```csharp
using Azure.AI.Vision.Face;

public class MediaAnonymizationService
{
    public async Task<Stream> BlurFacesInImage(Stream imageStream)
    {
        // Detect faces using Azure Face API
        var faceClient = new FaceClient(new Uri(_endpoint), new AzureKeyCredential(_key));
        var detectedFaces = await faceClient.DetectAsync(
            imageStream,
            FaceDetectionModel.Detection03,
            FaceRecognitionModel.Recognition04,
            returnFaceId: false
        );
        
        // Load image for processing
        using var image = Image.Load(imageStream);
        
        // Blur each detected face
        foreach (var face in detectedFaces.Value)
        {
            var faceRect = face.FaceRectangle;
            var region = new Rectangle(
                faceRect.Left,
                faceRect.Top,
                faceRect.Width,
                faceRect.Height
            );
            
            // Apply Gaussian blur to face region
            image.Mutate(x => x.GaussianBlur(20, region));
        }
        
        var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream);
        outputStream.Position = 0;
        
        return outputStream;
    }
    
    public async Task<Stream> BlurFacesInVideo(string videoUrl)
    {
        // Use Azure Video Indexer or custom Azure Media Services job
        var videoIndexerClient = new VideoIndexerClient(_accountId, _apiKey);
        
        // Start face redaction job
        var jobId = await videoIndexerClient.StartFaceRedactionJob(videoUrl);
        
        // Poll for completion
        var redactedVideoUrl = await videoIndexerClient.WaitForJobCompletion(jobId);
        
        return await DownloadVideoStream(redactedVideoUrl);
    }
}
```

**Voice Distortion**:
```csharp
public class AudioAnonymizationService
{
    public async Task<Stream> DistortVoice(Stream audioStream)
    {
        // Apply pitch shifting and formant modification
        // to make voice unrecognizable while preserving speech
        
        var ffmpegArgs = "-i pipe:0 " +
                        "-filter:a \"asetrate=44100*0.9,aresample=44100,atempo=1.11\" " +
                        "-f mp3 pipe:1";
        
        var outputStream = await RunFFmpegProcessAsync(audioStream, ffmpegArgs);
        return outputStream;
    }
}
```

**PII Redaction Tools**:
```csharp
public class PiiRedactionService
{
    public async Task<string> RedactTextPii(string text)
    {
        var textAnalyticsClient = new TextAnalyticsClient(
            new Uri(_endpoint),
            new AzureKeyCredential(_key)
        );
        
        // Recognize PII entities
        var response = await textAnalyticsClient.RecognizePiiEntitiesAsync(text);
        var redactedText = text;
        
        // Replace PII with [REDACTED] markers
        foreach (var entity in response.Value.OrderByDescending(e => e.Offset))
        {
            var category = entity.Category.ToString();
            redactedText = redactedText.Remove(entity.Offset, entity.Length)
                                      .Insert(entity.Offset, $"[{category}_REDACTED]");
        }
        
        return redactedText;
    }
    
    public async Task<Stream> RedactImagePii(Stream imageStream)
    {
        // Detect and blur license plates, addresses, phone numbers, documents
        using var image = Image.Load(imageStream);
        
        // Use Azure Computer Vision OCR to detect text
        var textRegions = await DetectTextRegions(imageStream);
        
        // Analyze text for PII patterns
        foreach (var region in textRegions)
        {
            if (ContainsPii(region.Text))
            {
                // Blur the region
                image.Mutate(x => x.GaussianBlur(30, region.BoundingBox));
            }
        }
        
        var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream);
        outputStream.Position = 0;
        
        return outputStream;
    }
    
    private bool ContainsPii(string text)
    {
        // Check for phone numbers, emails, addresses, license plates
        var patterns = new[]
        {
            @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", // Phone
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", // Email
            @"\b\d{5}(-\d{4})?\b", // ZIP code
            @"\b[A-Z]{1,2}\d{1,2}\s?\d{1,2}[A-Z]{2}\b" // License plate (UK)
        };
        
        return patterns.Any(pattern => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }
}
```

**Media Watermarking**:
```csharp
public class WatermarkingService
{
    public async Task<Stream> AddWatermarkToImage(Stream imageStream, string watermarkText)
    {
        using var image = Image.Load(imageStream);
        
        // Add semi-transparent watermark
        var font = SystemFonts.CreateFont("Arial", 48);
        var textOptions = new TextOptions(font)
        {
            Origin = new PointF(image.Width / 2, image.Height / 2),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        image.Mutate(x => x.DrawText(
            textOptions,
            watermarkText,
            Color.FromRgba(255, 255, 255, 128) // Semi-transparent white
        ));
        
        var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream);
        outputStream.Position = 0;
        
        return outputStream;
    }
}
```

**Access Control for Identifiable Media**:
```csharp
public class MediaAccessService
{
    public async Task<bool> CanAccessIdentifiableMedia(Guid userId, Guid mediaId)
    {
        var media = await _mediaRepository.GetById(mediaId);
        
        if (!media.ContainsIdentifiableContent)
        {
            // Non-identifiable media has standard access controls
            return await _authorizationService.CanAccessStudy(userId, media.StudyId);
        }
        
        // Identifiable media requires explicit permission
        var hasExplicitPermission = await _permissionRepository.HasMediaAccess(userId, mediaId);
        
        // Check consent allows this user to view
        var consent = await _consentRepository.GetByStudyAndParticipant(
            media.StudyId,
            media.ParticipantId
        );
        
        var consentAllows = consent.DataUsageAgreement.AllowMediaInReports;
        
        // Audit the access
        await _auditService.Log(new AuditLog
        {
            Action = "identifiable_media_access",
            UserId = userId,
            EntityType = "Media",
            EntityId = mediaId,
            Metadata = new 
            { 
                Granted = hasExplicitPermission && consentAllows,
                Reason = !consentAllows ? "Consent does not allow" : "Permission granted"
            }
        });
        
        return hasExplicitPermission && consentAllows;
    }
}
```

### HIPAA-Lite for Health Research

**When HIPAA Applies**:
- Research involving health conditions, medications, medical history
- Wearable device data (heart rate, sleep patterns, activity levels)
- Mental health research
- Healthcare provider feedback studies

**Protected Health Information (PHI) Safeguards**:
```csharp
public class PhiProtectionService
{
    private static readonly HashSet<string> PhiIdentifiers = new()
    {
        "Name", "Address", "DateOfBirth", "PhoneNumber", "Email",
        "SocialSecurityNumber", "MedicalRecordNumber", "HealthPlanNumber",
        "AccountNumber", "VehicleIdentifier", "DeviceIdentifier",
        "WebUrl", "IpAddress", "BiometricIdentifier", "FacePhotograph",
        "UniqueIdentifier"
    };
    
    public async Task<bool> IsPhiData(Guid studyId)
    {
        var study = await _studyRepository.GetById(studyId);
        return study.ResearchCategory == ResearchCategory.Health ||
               study.ResearchCategory == ResearchCategory.Medical;
    }
    
    public async Task<DeidentifiedData> DeidentifyPhiData(ParticipantData data)
    {
        var deidentified = new DeidentifiedData
        {
            // Replace exact dates with ranges
            AgeRange = CalculateAgeRange(data.DateOfBirth), // e.g., "35-40"
            LocationRegion = GetRegion(data.ZipCode), // e.g., "Northeast"
            
            // Remove all direct identifiers
            // Keep only data necessary for research
            Responses = data.Responses,
            Demographics = new
            {
                Gender = data.Gender,
                AgeRange = CalculateAgeRange(data.DateOfBirth),
                Region = GetRegion(data.ZipCode)
                // No name, address, phone, email, etc.
            }
        };
        
        return deidentified;
    }
    
    public async Task LogPhiAccess(Guid userId, Guid dataId, string accessReason)
    {
        await _auditService.Log(new AuditLog
        {
            Action = "phi_access",
            UserId = userId,
            EntityType = "ParticipantData",
            EntityId = dataId,
            Metadata = new 
            { 
                AccessReason = accessReason,
                Timestamp = DateTime.UtcNow,
                AccessType = "View"
            }
        });
    }
}
```

**Business Associate Agreement (BAA)**:
- Platform acts as Business Associate when handling PHI for covered entities
- Written BAA required before health research begins
- Documented security measures and breach notification procedures
- Sub-contractor agreements for third-party services (transcription, cloud storage)

**Minimum Necessary Standard**:
- Collect only PHI necessary for research purpose
- Limit access to PHI to authorized research team members
- Use de-identified data whenever possible

## Audit Logging

### Audit Event Types

- Authentication events (login, logout, MFA, password reset)
- Authorization failures (access denied)
- Data modifications (create, update, delete)
- Administrative actions (user suspension, role changes)
- Configuration changes (tenant settings, feature flags)
- Sensitive data access (viewing PII, exporting data)

### Audit Log Implementation

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } // "user.login", "post.delete", etc.
    public string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string OldValue { get; set; } // JSON snapshot
    public string NewValue { get; set; } // JSON snapshot
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        await _next(context);
        
        if (ShouldAudit(context))
        {
            await auditService.LogAsync(new AuditLog
            {
                TenantId = context.User.TenantId(),
                UserId = context.User.UserId(),
                Action = $"{context.Request.Method} {context.Request.Path}",
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"],
                Timestamp = DateTime.UtcNow
            });
        }
        
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
    
    private bool ShouldAudit(HttpContext context)
    {
        // Audit all POST, PUT, PATCH, DELETE
        if (context.Request.Method != "GET" && context.Request.Method != "HEAD")
        {
            return true;
        }
        
        // Audit sensitive GET endpoints
        var sensitivePaths = new[] { "/api/users", "/api/admin", "/api/analytics/export" };
        return sensitivePaths.Any(p => context.Request.Path.StartsWithSegments(p));
    }
}
```

### Audit Log Retention

- **Active Storage** (SQL Database): 90 days
- **Long-Term Storage** (Azure Blob Storage, append-only): 7 years
- **Immutable Storage**: Write-once, read-many (WORM) policy for compliance

## Compliance

### GDPR Compliance

**Data Subject Rights**:
- Right to access: API endpoint to export all user data
- Right to rectification: Profile editing capabilities
- Right to erasure: User deletion with data anonymization
- Right to data portability: Machine-readable export (JSON/CSV)
- Right to restrict processing: Account suspension without deletion
- Right to object: Opt-out of analytics and marketing

**Consent Management**:
```csharp
public class UserConsent
{
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } // "analytics", "marketing", "third-party-sharing"
    public bool IsGranted { get; set; }
    public DateTime GrantedAt { get; set; }
    public string IpAddress { get; set; }
}
```

### SOC 2 Type II

**Control Objectives**:
- Security: Protect against unauthorized access
- Availability: System accessible as committed
- Processing Integrity: Complete, valid, authorized processing
- Confidentiality: Confidential information protected
- Privacy: Personal information collected, used, retained as committed

**Evidence Collection**:
- Audit logs for all administrative actions
- Access reviews (quarterly)
- Vulnerability scans (monthly)
- Penetration tests (annual)
- Employee background checks
- Security awareness training records

## Security Monitoring

**Anomaly Detection**:
- Unusual login patterns (geo-location, time, device)
- High-volume API requests from single user
- Mass data export attempts
- Privilege escalation attempts
- Failed authentication spike

**Automated Response**:
- Temporary account lockout after failed login attempts
- CAPTCHA challenges for suspicious activity
- MFA requirement for sensitive operations
- Automatic alerts to security team

**Security Dashboard**:
- Failed login attempts by user and IP
- Privilege escalation events
- Data export activity
- API rate limit violations
- Suspicious tenant access patterns

