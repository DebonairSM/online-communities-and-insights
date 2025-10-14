# Getting Started with Online Communities SaaS

## What This Project Is

A multi-tenant SaaS application for building online research communities, built with:
- **Backend:** ASP.NET Core 9 (Clean Architecture)
- **Authentication:** OAuth 2.0 Social Login (Google, GitHub, Microsoft)
- **Database:** SQL Server with Entity Framework Core
- **Authorization:** Role-based access control (stored in YOUR database)

## Quick Start

### 1. Clone and Build

```bash
git clone <your-repo>
cd online-communities-and-insights
dotnet build
```

### 2. Run the API

```bash
cd src/Api
dotnet run
```

Visit: http://localhost:5000/health

### 3. Project Structure

```
src/
├── Core/              # Domain entities, interfaces (no dependencies)
├── Application/       # Business logic, services
├── Infrastructure/    # Data access, external services
└── Api/               # Web API, controllers, middleware

docs/
├── contexts/          # Domain and architecture contexts
├── implementation/    # Implementation guides
└── architecture/      # Architecture decision records
```

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│  API Layer (Controllers, Middleware)│
├─────────────────────────────────────┤
│  Application Layer (Services, DTOs) │
├─────────────────────────────────────┤
│  Core Layer (Entities, Interfaces)  │
└─────────────────────────────────────┘
         ▲
         │ implemented by
┌─────────────────────────────────────┐
│  Infrastructure Layer (EF Core, etc)│
└─────────────────────────────────────┘
```

### Multi-Tenant Model

Users can belong to multiple tenants with different roles:

```
User "Alice"
  ├── Tenant: "Acme Research" → Role: Admin
  └── Tenant: "Global Study" → Role: Member

User "Bob"
  └── Tenant: "Acme Research" → Role: Member
```

## Authentication Strategy

### Phase 1: Email/Password (Future)
Traditional registration with username and password.

### Phase 2: Social Login (Current Implementation)
Users log in with existing accounts:
- Google (users@gmail.com)
- GitHub (developers)
- Microsoft Personal Accounts (outlook.com, hotmail.com)

**No Microsoft Entra ID (Azure AD) tenants needed!**

### Phase 3: Enterprise SSO (Future)
When enterprise customers request it:
- Microsoft Entra ID (work accounts)
- Okta
- Custom SAML providers

## Key Concepts

### Tenant (Your Application)
A customer organization in YOUR database:
```sql
INSERT INTO Tenants (Name) VALUES ('Acme Research');
```
No Azure/Entra ID involved - just a database record!

### User
A person who uses your app:
```sql
INSERT INTO Users (Email, ExternalLoginProvider, ExternalUserId)
VALUES ('alice@gmail.com', 'Google', 'google-user-id-123');
```

### TenantMembership
Links users to tenants with roles:
```sql
INSERT INTO TenantMemberships (UserId, TenantId, RoleName)
VALUES (alice_id, acme_tenant_id, 'Admin');
```

## Authentication Flow (Social Login)

```
1. User clicks "Sign in with Google"
2. Redirects to Google OAuth
3. User authenticates with Google
4. Google redirects back with user info
5. Your API:
   - Creates user record if doesn't exist (JIT provisioning)
   - Issues JWT token for API access
6. User calls API with JWT token
7. Authorization checks roles in YOUR database
```

## Next Steps

1. **Set up OAuth providers:** See `docs/implementation/social-login-setup.md`
2. **Configure database:** See `docs/setup/development-environment.md`
3. **Understand domain model:** See `docs/contexts/domain-model.md`
4. **Review architecture:** See `docs/contexts/backend-architecture.md`

## Common Questions

**Q: Do I need Azure Entra ID?**  
A: No! Not for social login. Entra ID is only needed for enterprise SSO (Phase 3).

**Q: Where do users' passwords live?**  
A: They don't! Users authenticate with Google/GitHub/Microsoft. No passwords stored.

**Q: How do I create a new tenant?**  
A: Just insert a record in the Tenants table. No Azure provisioning needed.

**Q: What about MSAL?**  
A: MSAL is only for Entra ID (Phase 3). Use standard OAuth 2.0 for social login.

## Project Status

- ✅ Clean Architecture scaffold
- ✅ Multi-tenant data model
- ✅ Authorization framework (roles in database)
- 🚧 Social login authentication (in progress)
- ⏳ Database and EF Core setup
- ⏳ Frontend implementation
- ⏳ Deployment configuration

## Support Documentation

- **Implementation Guides:** `docs/implementation/`
- **Architecture Decisions:** `docs/architecture/decisions/`
- **Context Documents:** `docs/contexts/`
- **Templates:** `docs/templates/`

---

**Need Help?** Check the implementation guides in `docs/implementation/` or review the architecture decision records.

