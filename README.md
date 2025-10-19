# Online Communities & Insights SaaS Platform

Multi-tenant SaaS platform for building online research communities with Microsoft Entra External ID authentication.

## Quick Start

```bash
# Clone repository
git clone <your-repo-url>
cd online-communities-and-insights

# Build solution
cd backend
dotnet build

# Run API
cd src/Api
dotnet run

# Visit health check
curl https://localhost:5001/health
```

## What's Built (Phase 0 Complete ✅)

✅ **Clean Architecture** - Core, Application, Infrastructure, API layers  
✅ **Microsoft Entra External ID** - Enterprise-grade managed authentication  
✅ **JWT Token Validation** - Secure API access with Microsoft-issued tokens  
✅ **Multi-Tenant RBAC** - Users can belong to multiple tenants with different roles  
✅ **Database & Repositories** - Entity Framework Core with SQL Server  
✅ **74 Tests Passing** - Comprehensive unit and integration test coverage

## Authentication

### Microsoft Entra External ID
Enterprise-grade managed authentication with:
- Social identity providers (Google, GitHub, Microsoft accounts)
- Email/password sign-up
- Multi-factor authentication (MFA)
- Custom claims for tenant and role context
- SOC 2, FedRAMP, ISO 27001 compliance

### Authentication Flow

```
User signs in via Entra External ID
    ↓
Entra authenticates user
    ↓
Entra calls API Connector to enrich token
    ↓
Your API returns tenant ID and roles
    ↓
Entra issues JWT with custom claims
    ↓
Frontend uses token for API calls
    ↓
Your API validates token signature and checks roles
```

## Project Structure

```
backend/
├── src/
│   ├── Core/              # Domain entities, interfaces (no dependencies)
│   ├── Application/       # Business logic, CQRS, services
│   ├── Infrastructure/    # Data access, repositories, migrations
│   └── Api/               # Web API, controllers, authorization
├── tests/                 # Unit and integration tests
└── OnlineCommunities.sln  # Solution file

frontend/                  # React + Vite frontend application

docs/
├── GETTING-STARTED.md           # Start here
├── OVERVIEW.md                  # System overview
├── AZURE-CONFIGURATION-STEPS.md # Azure setup guide
├── backend/                     # Backend documentation
├── frontend/                    # Frontend documentation
└── implementation/              # Implementation tracking
```

## Configuration

### Azure Setup Required

Before running the application, you need to configure Microsoft Entra External ID:

1. **Create Entra External ID Tenant** - See `docs/AZURE-CONFIGURATION-STEPS.md`
2. **Configure Custom Attributes** - TenantId and Roles for token enrichment
3. **Set up API Connector** - For enriching tokens with tenant/role claims
4. **Create Azure SQL Database** - For storing user and tenant data

### Update appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://yourcompany.ciamlogin.com/",
    "TenantId": "your-tenant-id-guid",
    "ClientId": "your-client-id-guid",
    "Audience": "your-client-id-guid"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=OnlineCommunities;..."
  }
}
```

See `backend/src/Api/appsettings.Example.json` for full template.

## API Endpoints

### Authentication
- `GET /` - Landing page with API information
- `GET /health` - Health check
- `GET /api/auth/me` - Get current user profile (requires JWT token)
- `GET /api/auth/status` - Check authentication status
- `GET /api/auth/validate-token` - Validate JWT token
- `POST /api/auth/signout` - Sign out (client-side token deletion)
- `POST /api/entra-connector/token-enrichment` - Token enrichment for Entra (called by Microsoft)

### Users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## Frontend Integration

Frontend authentication uses MSAL (Microsoft Authentication Library) to integrate with Entra External ID:

```typescript
import { PublicClientApplication } from '@azure/msal-browser';

const msalConfig = {
  auth: {
    clientId: 'your-client-id',
    authority: 'https://yourcompany.ciamlogin.com/yourcompany.onmicrosoft.com/b2c_1_signupsignin',
    redirectUri: window.location.origin,
  }
};

const msalInstance = new PublicClientApplication(msalConfig);

// Sign in
await msalInstance.loginPopup();

// Get access token
const accounts = msalInstance.getAllAccounts();
const response = await msalInstance.acquireTokenSilent({
  scopes: ['openid', 'profile', 'email'],
  account: accounts[0]
});

// Use token for API calls
const apiResponse = await fetch('/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${response.accessToken}`
  }
});
```

## Documentation

- **Getting Started**: `docs/GETTING-STARTED.md` - Start here for development setup
- **System Overview**: `docs/OVERVIEW.md` - Architecture and design
- **Azure Setup**: `docs/AZURE-CONFIGURATION-STEPS.md` - Quick Azure configuration guide
- **Backend Architecture**: `docs/backend/README.md` - Backend design and patterns
- **CQRS Guide**: `docs/CQRS-IMPLEMENTATION-SUMMARY.md` - CQRS pattern implementation
- **Implementation Status**: `docs/implementation/status.md` - Current progress
- **Phase 0 Complete**: `docs/implementation/PHASE-0-COMPLETE.md` - Foundation summary

## Development Status

### Phase 0: Foundation (✅ Complete)
- ✅ Clean Architecture structure
- ✅ Microsoft Entra External ID authentication
- ✅ JWT token validation
- ✅ Multi-tenant data model
- ✅ Entity Framework Core with SQL Server
- ✅ Repository pattern implementations
- ✅ Authorization framework with custom policies
- ✅ 74 unit and integration tests passing
- ✅ CQRS implementation with custom mediator

### Phase 1: Core Community Features (🚧 Next)
- Frontend application with authentication
- Community management
- Content creation and engagement
- Basic research tools

## Tech Stack

- **.NET 9** - Web API framework
- **Microsoft Entra External ID** - Managed authentication with social providers
- **JWT Bearer** - Token validation
- **Entity Framework Core 9** - Data access and migrations
- **SQL Server** - Database
- **Clean Architecture** - Separation of concerns
- **CQRS Pattern** - Custom mediator implementation

## Next Steps

1. **Configure Azure** - See `docs/AZURE-CONFIGURATION-STEPS.md`
2. **Run Tests** - `cd backend && dotnet test`
3. **Start Building** - Review `docs/implementation/roadmap.md` for Phase 1 features

## License

[Your License Here]

