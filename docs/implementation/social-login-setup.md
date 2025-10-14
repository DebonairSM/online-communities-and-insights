# Social Login Implementation Guide

## Overview

This guide explains how to implement OAuth 2.0 social login for your SaaS application. Users can sign in with:
- Google (Gmail accounts)
- GitHub (Developer accounts)
- Microsoft Personal Accounts (Outlook, Hotmail)

**No Microsoft Entra ID or MSAL needed!** This uses standard OAuth 2.0 flows.

## Architecture

```
User clicks "Sign in with Google"
           ↓
Redirect to Google OAuth
           ↓
User authenticates with Google
           ↓
Google redirects back with user info
           ↓
Your API:
  - Looks up user by (Provider + ExternalUserId)
  - If not found → Create user (JIT provisioning)
  - Generate JWT token
           ↓
Return JWT to frontend
           ↓
Frontend uses JWT for API calls
```

## User Entity Design (Future-Proof)

The User entity supports multiple authentication methods:

```csharp
public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    // Authentication method
    public AuthenticationMethod AuthMethod { get; set; }
    
    // Phase 1: Email/Password (future)
    public string? PasswordHash { get; set; }
    
    // Phase 2: Social Login (current)
    public string? ExternalLoginProvider { get; set; }  // "Google", "GitHub"
    public string? ExternalUserId { get; set; }
    
    // Phase 3: Enterprise SSO (future)
    public string? EntraTenantId { get; set; }
    public string? EntraIdSubject { get; set; }
    
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; }
    
    public ICollection<TenantMembership> TenantMemberships { get; set; }
}

public enum AuthenticationMethod
{
    EmailPassword = 1,
    Google = 2,
    GitHub = 3,
    Microsoft = 4,
    EntraId = 5  // Future
}
```

## Setup OAuth Providers

### 1. Google OAuth Setup

**Create OAuth credentials:**
1. Go to https://console.cloud.google.com/
2. Create new project or select existing
3. Navigate to "APIs & Services" → "Credentials"
4. Click "Create Credentials" → "OAuth 2.0 Client ID"
5. Application type: "Web application"
6. Authorized redirect URIs: `https://yourdomain.com/signin-google`
   - For local dev: `https://localhost:5001/signin-google`
7. Copy **Client ID** and **Client Secret**

### 2. GitHub OAuth Setup

**Create OAuth App:**
1. Go to https://github.com/settings/developers
2. Click "New OAuth App"
3. Application name: Your app name
4. Homepage URL: `https://yourdomain.com`
5. Authorization callback URL: `https://yourdomain.com/signin-github`
   - For local dev: `https://localhost:5001/signin-github`
6. Copy **Client ID** and **Client Secret**

### 3. Microsoft Personal Account Setup

**Register application:**
1. Go to https://portal.azure.com
2. Navigate to "Microsoft Entra ID" → "App registrations"
3. Click "New registration"
4. Name: Your app name
5. Supported account types: **"Personal Microsoft accounts only"**
   - NOT "Organizational accounts" (that's Microsoft Entra ID for enterprise/Phase 3)
6. Redirect URI: Web - `https://yourdomain.com/signin-microsoft`
7. Copy **Application (client) ID** and create a **Client Secret**

**Note:** This is for consumer accounts (outlook.com, hotmail.com, live.com) only. For enterprise work accounts, see Phase 3.

## Configuration

**appsettings.json:**
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    }
  },
  "JwtSettings": {
    "SecretKey": "your-very-long-secret-key-at-least-32-characters",
    "Issuer": "OnlineCommunitiesAPI",
    "Audience": "OnlineCommunitiesUsers",
    "ExpiryMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OnlineCommunities;Trusted_Connection=true"
  }
}
```

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "dev-google-client-id",
      "ClientSecret": "dev-google-secret"
    },
    "GitHub": {
      "ClientId": "dev-github-client-id",
      "ClientSecret": "dev-github-secret"
    },
    "Microsoft": {
      "ClientId": "dev-microsoft-client-id",
      "ClientSecret": "dev-microsoft-secret"
    }
  }
}
```

## NuGet Packages Required

```bash
# OAuth providers (built into ASP.NET Core)
dotnet add src/Api package Microsoft.AspNetCore.Authentication.Google
dotnet add src/Api package Microsoft.AspNetCore.Authentication.GitHub

# Microsoft personal accounts (NOT Entra ID)
dotnet add src/Api package Microsoft.AspNetCore.Authentication.MicrosoftAccount

# JWT tokens
dotnet add src/Api package System.IdentityModel.Tokens.Jwt
dotnet add src/Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

## Implementation Flow

### 1. User Entity
Already created at `src/Core/Entities/Identity/User.cs` - needs updating for flexible auth.

### 2. ExternalAuthService
Service that handles:
- Looking up users by external login
- Creating new users (JIT provisioning)
- Updating existing users
- Generating JWT tokens

### 3. AuthController
API endpoints for:
- Initiating OAuth flow (`/api/auth/login/{provider}`)
- OAuth callbacks (`/signin-google`, `/signin-github`, `/signin-microsoft`)
- Getting current user (`/api/auth/me`)

### 4. Program.cs Configuration
Register:
- OAuth authentication providers
- JWT bearer authentication
- ExternalAuthService
- User repositories

## Frontend Integration

### React Example

```tsx
import { useState } from 'react';

function LoginPage() {
  const handleSocialLogin = (provider: 'google' | 'github' | 'microsoft') => {
    // Redirect to your API endpoint
    window.location.href = `/api/auth/login/${provider}`;
  };

  return (
    <div>
      <h1>Sign In</h1>
      
      <button onClick={() => handleSocialLogin('google')}>
        <img src="/google-icon.svg" /> Sign in with Google
      </button>
      
      <button onClick={() => handleSocialLogin('github')}>
        <img src="/github-icon.svg" /> Sign in with GitHub
      </button>
      
      <button onClick={() => handleSocialLogin('microsoft')}>
        <img src="/microsoft-icon.svg" /> Sign in with Microsoft
      </button>
    </div>
  );
}
```

After successful login:
```tsx
// API returns JWT token
const response = await fetch('/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  }
});
```

## Database Schema

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    
    -- Authentication method
    AuthMethod INT NOT NULL,
    
    -- Email/Password (Phase 1 - future)
    PasswordHash NVARCHAR(255) NULL,
    
    -- Social Login (Phase 2 - current)
    ExternalLoginProvider NVARCHAR(50) NULL,
    ExternalUserId NVARCHAR(255) NULL,
    
    -- Enterprise SSO (Phase 3 - future)
    EntraTenantId NVARCHAR(100) NULL,
    EntraIdSubject NVARCHAR(255) NULL,
    
    EmailVerified BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    
    CONSTRAINT UK_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_AuthMethod CHECK (
        (AuthMethod = 1 AND PasswordHash IS NOT NULL) OR
        (AuthMethod IN (2,3,4) AND ExternalLoginProvider IS NOT NULL AND ExternalUserId IS NOT NULL) OR
        (AuthMethod = 5 AND EntraIdSubject IS NOT NULL)
    )
);

-- Index for social login lookups
CREATE INDEX IX_Users_ExternalLogin 
ON Users(ExternalLoginProvider, ExternalUserId);

-- Index for future enterprise SSO
CREATE INDEX IX_Users_EntraId 
ON Users(EntraIdSubject) 
WHERE EntraIdSubject IS NOT NULL;
```

## Testing

### Local Testing

1. **Start API:**
   ```bash
   cd src/Api
   dotnet run
   ```

2. **Navigate to login:**
   ```
   https://localhost:5001/api/auth/login/google
   ```

3. **Authenticate with Google**

4. **Should redirect back with JWT token**

5. **Test authenticated endpoint:**
   ```bash
   curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
        https://localhost:5001/api/auth/me
   ```

### Common Issues

**"Redirect URI mismatch"**
- Ensure redirect URI in OAuth provider matches exactly
- Include protocol (https://) and port number
- No trailing slashes

**"Invalid client secret"**
- Double-check client secret in appsettings
- Regenerate secret in OAuth provider if needed

**"Email claim not found"**
- Add email scope to OAuth configuration
- Check provider documentation for claim names

## Security Considerations

1. **HTTPS Required:** OAuth requires HTTPS (except localhost)
2. **Secure Secrets:** Never commit appsettings.json with real secrets
3. **CORS:** Configure CORS properly for frontend domain
4. **Token Expiry:** Set reasonable JWT expiry times
5. **Refresh Tokens:** Implement refresh token flow for production

## Future: Adding Enterprise SSO (Phase 3)

When enterprise customers request work account SSO, you can add Microsoft Entra ID support:

1. **Add MSAL package:**
   ```bash
   dotnet add src/Api package Microsoft.Identity.Web
   ```

2. **Update configuration for multi-tenant Entra ID:**
   ```json
   "EntraId": {
     "Instance": "https://login.microsoftonline.com/",
     "TenantId": "common",
     "ClientId": "your-client-id",
     "ClientSecret": "your-client-secret"
   }
   ```

3. **Add authentication scheme:**
   ```csharp
   .AddMicrosoftIdentityWebApi(config.GetSection("EntraId"));
   ```

4. **Use existing nullable fields** (`EntraTenantId`, `EntraIdSubject`)

**No refactoring needed!** The User entity is already prepared for all authentication methods.

### Alternative: Microsoft Entra External ID (for consumer auth with email/password)

If you want to add email/password authentication (Phase 1) using a Microsoft managed service:

1. **Create Microsoft Entra External ID tenant** (formerly Azure AD B2C)
2. **Configure user flows** for sign-up/sign-in
3. **Add social identity providers** within External ID
4. **One service handles everything:** email/password + social login + future enterprise SSO

This consolidates all authentication into Microsoft Entra External ID instead of managing multiple OAuth providers separately.

## Next Steps

1. Update User entity with authentication fields
2. Create ExternalAuthService
3. Create AuthController
4. Update Program.cs
5. Test with each provider
6. Integrate with frontend

