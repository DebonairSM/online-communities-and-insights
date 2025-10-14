# Online Communities & Insights SaaS Platform

Multi-tenant SaaS platform for building online research communities with social login authentication.

## Quick Start

```bash
# Clone repository
git clone <your-repo-url>
cd online-communities-and-insights

# Build solution
dotnet build

# Run API
cd src/Api
dotnet run

# Visit health check
curl https://localhost:5001/health
```

## What's Built

✅ **Clean Architecture** - Core, Application, Infrastructure, API layers  
✅ **Social Login** - Google, GitHub, Microsoft Personal Accounts  
✅ **JWT Authentication** - Secure API access tokens  
✅ **Multi-Tenant** - Users can belong to multiple tenants with different roles  
✅ **Flexible Auth Design** - Future-proof for email/password and enterprise SSO  

## Authentication

### Phase 2: Social Login (Current)
Users log in with existing accounts:
- **Google** - Gmail and Google Workspace personal accounts
- **GitHub** - Developer accounts
- **Microsoft** - Outlook.com, Hotmail.com accounts

**No Microsoft Entra ID needed!** (That's Phase 3 for enterprise customers)

### Authentication Flow

```
User clicks "Sign in with Google"
    ↓
Redirects to Google OAuth
    ↓
User authenticates
    ↓
Google redirects back with user info
    ↓
Your API creates user record (JIT provisioning)
    ↓
Returns JWT token
    ↓
Frontend uses token for API calls
```

## Project Structure

```
src/
├── Core/              # Domain entities, enums, interfaces
├── Application/       # Business logic, services (ExternalAuthService)
├── Infrastructure/    # Data access, repositories (stubs)
└── Api/               # Web API, controllers, authentication

docs/
├── GETTING-STARTED.md           # Start here
├── PROJECT-STRUCTURE.md         # Architecture details
└── implementation/
    └── social-login-setup.md    # OAuth setup guide
```

## Configuration

### 1. Set Up OAuth Providers

**Google:**
- Go to https://console.cloud.google.com/
- Create OAuth 2.0 Client ID
- Copy Client ID and Secret

**GitHub:**
- Go to https://github.com/settings/developers
- Create OAuth App
- Copy Client ID and Secret

**Microsoft:**
- Go to https://portal.azure.com
- Create App Registration (Personal accounts only)
- Copy Client ID and Secret

### 2. Update appsettings.json

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
    "SecretKey": "change-this-to-a-long-random-string-32-chars-minimum"
  }
}
```

See `appsettings.Example.json` for full template.

### 3. Set Up Database (TODO)

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Create DbContext (see docs/implementation/social-login-setup.md)

# Run migrations
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

### 4. Implement UserRepository (TODO)

Repository methods are stubbed. You need to:
1. Create `ApplicationDbContext`
2. Implement UserRepository methods
3. Register in Program.cs

See `src/Infrastructure/Repositories/UserRepository.cs` for TODOs.

## API Endpoints

### Authentication
- `GET /api/auth/login/{provider}` - Initiate OAuth login (google, github, microsoft)
- `GET /api/auth/callback/{provider}` - OAuth callback (redirects here after login)
- `GET /api/auth/me` - Get current user (requires JWT token)
- `GET /api/auth/status` - Check authentication status
- `POST /api/auth/signout` - Sign out

### Health Check
- `GET /health` - API health status

## Frontend Integration

### React Example

```tsx
function LoginPage() {
  const handleSocialLogin = (provider) => {
    // Redirect to your API
    window.location.href = `/api/auth/login/${provider}`;
  };

  return (
    <div>
      <button onClick={() => handleSocialLogin('google')}>
        Sign in with Google
      </button>
      <button onClick={() => handleSocialLogin('github')}>
        Sign in with GitHub
      </button>
      <button onClick={() => handleSocialLogin('microsoft')}>
        Sign in with Microsoft
      </button>
    </div>
  );
}
```

After login, API returns JWT token. Store it and use for API calls:

```tsx
// Call authenticated endpoint
const response = await fetch('/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  }
});
```

## Documentation

- **Getting Started**: `docs/GETTING-STARTED.md`
- **Project Structure**: `docs/PROJECT-STRUCTURE.md`
- **Social Login Setup**: `docs/implementation/social-login-setup.md`
- **Architecture Decisions**: `docs/architecture/decisions/`

## Development Status

- ✅ Clean Architecture scaffold
- ✅ Social login authentication
- ✅ JWT token generation
- ✅ Multi-tenant data model
- ✅ Authorization framework
- 🚧 Database setup (TODO)
- 🚧 UserRepository implementation (TODO)
- ⏳ Frontend (future)
- ⏳ Email/password auth (Phase 1 - future)
- ⏳ Enterprise SSO (Phase 3 - future)

## Tech Stack

- **.NET 9** - Web API framework
- **ASP.NET Core Authentication** - OAuth 2.0 providers (Google, GitHub, Microsoft)
- **JWT** - API authentication tokens
- **Entity Framework Core** - Data access (to be implemented)
- **SQL Server** - Database (to be implemented)
- **Clean Architecture** - Separation of concerns

## Microsoft Entra Clarification

**Microsoft Entra ID** (formerly Azure AD) = Enterprise workforce authentication (Phase 3 - future)  
**Microsoft Entra External ID** (formerly Azure AD B2C) = Consumer authentication (Phase 1 option - future)  

**Current Implementation:** Standard OAuth 2.0 (no Entra needed!)
- ✅ Works with personal accounts (Gmail, Outlook, GitHub)
- ✅ No Entra ID tenant management or creation
- ✅ No MSAL library needed (that's for enterprise SSO)
- ✅ Simple standard OAuth 2.0 flow
- ✅ Your SaaS tenants are just database records

**See `docs/AUTHENTICATION-STRATEGY.md` for complete clarification of Microsoft Entra terminology and authentication phases.**

## Key Documentation

Start here:
1. **`IMPLEMENTATION-STATUS.md`** - Current status and what's implemented
2. **`docs/AUTHENTICATION-STRATEGY.md`** - Microsoft Entra terminology clarified
3. **`docs/GETTING-STARTED.md`** - Getting started guide
4. **`docs/implementation/social-login-setup.md`** - OAuth setup instructions
5. **`docs/PROJECT-STRUCTURE.md`** - Architecture details

## Support

For questions:
- **Authentication confusion?** See `docs/AUTHENTICATION-STRATEGY.md`
- **Getting started?** See `docs/GETTING-STARTED.md`
- **OAuth setup?** See `docs/implementation/social-login-setup.md`
- **Architecture questions?** See `docs/contexts/backend-architecture.md`

## License

[Your License Here]

