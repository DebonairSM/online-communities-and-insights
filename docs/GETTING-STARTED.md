# Getting Started with Online Communities SaaS

## What This Project Is

A multi-tenant SaaS application for building online research communities, built with:
- **Backend:** ASP.NET Core 9 (Clean Architecture)
- **Authentication:** Microsoft Entra External ID (enterprise-grade managed authentication)
- **Database:** SQL Server with Entity Framework Core
- **Authorization:** Multi-tenant role-based access control

## Quick Start

### 1. Clone and Build

```bash
git clone <your-repo>
cd online-communities-and-insights
dotnet build
```

### 2. Run the API

```bash
cd backend/src/Api
dotnet run
```

Visit: http://localhost:5000/health

### 3. Project Structure

```
backend/
├── src/
│   ├── Core/              # Domain entities, interfaces (no dependencies)
│   ├── Application/       # Business logic, services
│   ├── Infrastructure/    # Data access, external services
│   └── Api/               # Web API, controllers, middleware
├── tests/                 # Unit and integration tests
└── OnlineCommunities.sln  # Solution file

frontend/                  # Frontend application

docs/
├── contexts/              # Domain and architecture contexts
├── implementation/        # Implementation guides
└── architecture/          # Architecture decision records
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

### Current: Microsoft Entra External ID
Enterprise-grade managed authentication with:
- Social identity providers (Google, GitHub, Microsoft Personal Accounts)
- Email/password sign-up
- Multi-factor authentication (MFA)
- Compliance certifications (SOC 2, FedRAMP, ISO 27001)
- Custom claims for tenant and role context

**Benefits:**
- No password management in your database
- Built-in breach detection and threat protection
- Automatic token lifecycle management
- Scalable to millions of users

### Future: Enterprise SSO
Additional enterprise authentication options:
- Microsoft Entra ID (work accounts) for B2B scenarios
- Okta, Auth0
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
INSERT INTO Users (Email, EntraIdSubject, AuthMethod)
VALUES ('alice@gmail.com', 'entra-oid-guid', 'EntraExternalId');
```

### TenantMembership
Links users to tenants with roles:
```sql
INSERT INTO TenantMemberships (UserId, TenantId, RoleName)
VALUES (alice_id, acme_tenant_id, 'Admin');
```

## Authentication Flow (Microsoft Entra External ID)

```
1. User clicks "Sign in" on your frontend
2. Redirects to Microsoft Entra External ID
3. User chooses authentication method (Google, GitHub, Microsoft, Email)
4. Entra authenticates the user
5. Entra calls your API Connector to enrich token with tenant/role claims
6. Your API returns tenant ID and roles from YOUR database
7. Entra includes custom claims in JWT token
8. User calls API with Entra-issued JWT token
9. Your API validates token signature and checks roles
```

## Next Steps

1. **Configure Azure:** See `AZURE-CONFIGURATION-STEPS.md` in docs folder
2. **Set up development environment:** See `docs/setup/development-environment.md`
3. **Understand domain model:** See `docs/backend/domain-model.md`
4. **Review architecture:** See `docs/backend/README.md`

## Common Questions

**Q: Do I need to configure Azure?**  
A: Yes! You need to set up Microsoft Entra External ID tenant, app registration, and custom attributes. See `docs/AZURE-CONFIGURATION-STEPS.md`.

**Q: Where do users' passwords live?**  
A: In Microsoft Entra External ID. Your database only stores user references and tenant relationships.

**Q: How do I create a new tenant?**  
A: Insert a record in the Tenants table. The tenant/role information is added to user tokens via API Connector.

**Q: What about MSAL?**  
A: MSAL is used on the frontend to handle Entra External ID authentication flows.

## Project Status

- ✅ Clean Architecture scaffold
- ✅ Multi-tenant data model
- ✅ Authorization framework with custom policies
- ✅ Microsoft Entra External ID authentication
- ✅ Database and EF Core setup
- ✅ 64 tests passing (unit + integration)
- ⏳ Frontend implementation
- ⏳ Azure deployment configuration

## Support Documentation

- **Implementation Guides:** `docs/implementation/`
- **Architecture Decisions:** `docs/backend/architecture-decisions.md`
- **Backend Documentation:** `docs/backend/`
- **Frontend Documentation:** `docs/frontend/`
- **Templates:** `docs/templates/`

---

**Need Help?** Check the implementation guides in `docs/implementation/` or review the architecture decision records.

