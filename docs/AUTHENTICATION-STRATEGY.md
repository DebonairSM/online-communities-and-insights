# Authentication Strategy - Clarified

## Microsoft Entra Terminology (2024/2025)

Microsoft rebranded Azure Active Directory in 2023. Here's the current naming:

| Old Name (Pre-2023) | Current Name (2024+) | Purpose |
|---------------------|---------------------|---------|
| **Azure AD** | **Microsoft Entra ID** | Enterprise workforce authentication (work/school accounts) |
| **Azure AD B2C** | **Microsoft Entra External ID** | Consumer/customer authentication (email/password + social login) |
| **Azure AD Multi-Tenant** | **Microsoft Entra ID** (multi-tenant mode) | SaaS apps accepting multiple enterprise customers |

## Your SaaS Tenants vs. Microsoft Entra Tenants

### CRITICAL: These Are Different Things!

**YOUR Application Tenant:**
- Just a database record in YOUR SQL database
- Represents a customer organization using your SaaS
- Created with: `INSERT INTO Tenants (Name) VALUES ('Acme Corp')`
- No Azure provisioning needed!
- Example: "Acme Research", "Globex Study"

**Microsoft Entra Tenant:**
- Microsoft's identity infrastructure
- Only needed for Phase 3 (Enterprise SSO)
- Your enterprise customers already have these (if they use Microsoft 365)
- YOU NEVER CREATE THESE!

## Your Authentication Phases

### Phase 1: Email/Password (Future - Not Implemented)

**Option A: Build it yourself**
```
User signs up with email + password
  ↓
Hash password with bcrypt
  ↓
Store in YOUR database
  ↓
Issue JWT token on login
```

**Pros:**
- Full control
- No external dependencies

**Cons:**
- Must build: password reset, email verification, MFA
- Security is your responsibility
- Not cloud-native

**Option B: Use Microsoft Entra External ID** (Recommended for cloud-native)
```
User signs up via Microsoft Entra External ID
  ↓
Microsoft handles: email/password, password reset, MFA
  ↓
Returns JWT token to your app
  ↓
Your app creates user record (JIT provisioning)
```

**Pros:**
- Cloud-native Azure managed service
- Security handled by Microsoft
- Built-in features (MFA, breach detection, password reset)
- Can also handle social login
- Free for first 50,000 monthly active users

**Cons:**
- Learning curve for configuration
- Microsoft-managed UI (some customization limits)

### Phase 2: Social Login (CURRENT - Implemented)

```
User clicks "Sign in with Google"
  ↓
Google OAuth authentication
  ↓
Google redirects with user info
  ↓
Your API:
  - Looks up user by (Provider + ExternalUserId)
  - Creates user if doesn't exist
  - Issues YOUR OWN JWT token
  ↓
Frontend uses JWT for API calls
```

**What's Implemented:**
- ✅ Google OAuth
- ✅ GitHub OAuth
- ✅ Microsoft Personal Accounts (outlook.com, hotmail.com)
- ✅ JWT token generation and validation
- ✅ JIT (Just-In-Time) user provisioning
- ✅ Future-proof User entity

**No MSAL or Entra ID Needed!**
- Uses standard OAuth 2.0
- Works with any OAuth provider
- Perfect for consumer/SMB SaaS

### Phase 3: Enterprise SSO (Future - Not Implemented)

When enterprise customers say: "We want our employees to use their work accounts"

**Microsoft Entra ID (Multi-Tenant):**
```
Customer already has Microsoft Entra ID tenant
  (They use Microsoft 365 for email, etc.)
  ↓
Your app configured as multi-tenant
  ↓
Customer IT admin grants consent
  ↓
Their employees can log in with work accounts
  ↓
Your API receives JWT with their Entra ID details
```

**Key Points:**
- Customers ALREADY HAVE Entra ID (you don't create it!)
- You configure YOUR app as multi-tenant
- Each customer's IT admin grants consent once
- Their users authenticate against THEIR Entra ID
- You still don't create any Entra ID tenants

**When to Implement:**
- Only when enterprise customers request it
- Not needed for consumers/SMBs
- Adds complexity - wait until you need it

## Current Implementation Details

### What You Built (Phase 2)

**Files Created:**
- `src/Core/Enums/AuthenticationMethod.cs` - Enum for all auth methods
- `src/Core/Entities/Identity/User.cs` - Flexible user entity
- `src/Application/Interfaces/IExternalAuthService.cs` - Service interface
- `src/Application/Services/Identity/ExternalAuthService.cs` - OAuth implementation
- `src/Api/Controllers/AuthController.cs` - OAuth endpoints
- `src/Api/Program.cs` - JWT + OAuth configuration

**Database:**
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL,
    AuthMethod INT NOT NULL,
    
    -- Phase 2 (current)
    ExternalLoginProvider NVARCHAR(50),  -- "Google", "GitHub", "Microsoft"
    ExternalUserId NVARCHAR(255),
    
    -- Phase 1 (future)
    PasswordHash NVARCHAR(255),
    
    -- Phase 3 (future)
    EntraTenantId NVARCHAR(100),
    EntraIdSubject NVARCHAR(255)
);
```

**Configuration (appsettings.json):**
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-secret"
    }
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "OnlineCommunitiesAPI",
    "Audience": "OnlineCommunitiesUsers",
    "ExpiryMinutes": 60
  }
}
```

## Adding Phase 1 (Email/Password)

### If Using Microsoft Entra External ID

1. **Create Entra External ID tenant in Azure Portal**
2. **Configure user flows** (sign-up/sign-in with email/password)
3. **Optionally add social providers** within External ID
4. **Update Program.cs:**
   ```csharp
   builder.Services.AddAuthentication()
       .AddMicrosoftIdentityWebApi(config.GetSection("EntraExternalId"));
   ```

### If Building It Yourself

1. **Install bcrypt:** `dotnet add package BCrypt.Net-Next`
2. **Create registration endpoint** that hashes passwords
3. **Create login endpoint** that verifies hash and issues JWT
4. **Implement password reset** with email tokens
5. **Implement email verification** workflow
6. **Add breach detection** (HaveIBeenPwned API)
7. **Add MFA** (TOTP with QR codes)

## Adding Phase 3 (Enterprise SSO)

1. **Create multi-tenant app registration** in Azure Portal
2. **Set TenantId to "common"** in configuration
3. **Install MSAL:** `dotnet add package Microsoft.Identity.Web`
4. **Add authentication scheme** alongside existing OAuth
5. **Customer admin grants consent** for their organization
6. **Extract `tid` claim** (their Entra tenant ID)
7. **Store in `EntraTenantId` field** (already exists in User entity)

## Decision Matrix

| Scenario | Recommendation |
|----------|----------------|
| **Consumer SaaS** (current) | ✅ OAuth 2.0 social login (implemented) |
| **Need email/password** | Option A: Microsoft Entra External ID<br>Option B: Build yourself |
| **Enterprise customers request SSO** | Microsoft Entra ID multi-tenant |
| **Cloud-native Azure** | Microsoft Entra External ID for all consumer auth |
| **Want simplicity** | Keep current OAuth implementation |
| **Want managed security** | Microsoft Entra External ID |

## Key Takeaways

1. **NO Entra ID tenants to create** - Just database records for your SaaS tenants
2. **NO MSAL for Phase 2** - Standard OAuth 2.0 only
3. **Microsoft Entra External ID** = Managed consumer authentication service (optional)
4. **Microsoft Entra ID** = Enterprise SSO (only for Phase 3)
5. **Your implementation is correct** - OAuth 2.0 social login is standard and appropriate
6. **Future-proof design** - Can add Phase 1 or Phase 3 without refactoring

## Next Steps

**To Complete Phase 2 (Current):**
1. Install OAuth NuGet packages
2. Set up OAuth provider credentials
3. Update appsettings.json with secrets
4. Create DbContext and run migrations
5. Implement UserRepository methods
6. Test OAuth login flow

**To Add Phase 1 (Future):**
- Option A: Set up Microsoft Entra External ID
- Option B: Build email/password yourself

**To Add Phase 3 (Future):**
- Configure Microsoft Entra ID multi-tenant
- Add MSAL package
- Update authentication configuration
- No database changes needed!

## Documentation References

- **Getting Started:** `docs/GETTING-STARTED.md`
- **OAuth Setup:** `docs/implementation/social-login-setup.md`
- **Project Structure:** `docs/PROJECT-STRUCTURE.md`
- **Tech Stack:** `docs/contexts/TechStack.context.md`
- **Security Model:** `docs/contexts/security-model.md`

---

**Remember:** Microsoft Entra ID ≠ Microsoft Entra External ID. They're different products for different scenarios!

