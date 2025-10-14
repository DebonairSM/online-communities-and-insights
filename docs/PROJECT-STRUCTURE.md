# Project Structure

## Solution Organization

```
OnlineCommunities.sln
├── src/
│   ├── Core/                    # Domain Layer (no dependencies)
│   ├── Application/             # Business Logic Layer
│   ├── Infrastructure/          # Data Access & External Services
│   └── Api/                     # Web API Layer
└── tests/
    ├── Core.Tests/
    ├── Application.Tests/
    └── Integration.Tests/
```

## Clean Architecture

This project follows **Clean Architecture** as documented in [ADR-001](architecture/decisions/001-use-clean-architecture.md):

- **Core** has NO dependencies (pure domain logic)
- **Application** depends only on Core
- **Infrastructure** implements interfaces from Core/Application
- **Api** orchestrates and depends on Application + Infrastructure

## Layer Details

### Core Layer (`src/Core/`)
**Purpose:** Domain entities, business rules, interfaces

**Key Files:**
- `Entities/Common/BaseEntity.cs` - Base class for all entities
- `Entities/Identity/User.cs` - User entity
- `Entities/Identity/TenantMembership.cs` - User-Tenant-Role mapping
- `Entities/Tenants/Tenant.cs` - Tenant entity
- `Interfaces/IRepository.cs` - Generic repository interface
- `Interfaces/IUserRepository.cs` - User-specific queries
- `Interfaces/ITenantMembershipRepository.cs` - Role checking interface

**Dependencies:** NONE (pure domain logic)

### Application Layer (`src/Application/`)
**Purpose:** Business logic, use cases, service interfaces

**Key Files:**
- `Interfaces/` - Service contracts
- `Services/Identity/` - Authentication and role management
- `Services/Research/` - Survey, diary services (future)
- `Services/Moderation/` - Content moderation (future)
- `Services/Analytics/` - Analytics and insights (future)
- `Commands/` - CQRS command handlers (future)
- `Queries/` - CQRS query handlers (future)
- `DTOs/` - Data transfer objects
- `Validators/` - FluentValidation validators (future)

**Dependencies:** Core only

### Infrastructure Layer (`src/Infrastructure/`)
**Purpose:** External services, data access, third-party integrations

**Folders:**
- `Data/` - EF Core DbContext, configurations, migrations
- `Repositories/` - Repository implementations
- `Identity/` - Authentication providers (future)
- `Integrations/` - External APIs (future)
- `Messaging/` - Event bus, message queue (future)

**Dependencies:** Core, Application

### API Layer (`src/Api/`)
**Purpose:** HTTP endpoints, middleware, startup configuration

**Key Files:**
- `Program.cs` - Application startup and DI configuration
- `Controllers/` - API endpoints
- `Middleware/` - Custom middleware
- `Authorization/` - Authorization handlers and requirements
- `Extensions/` - Helper extension methods
- `Filters/` - Action filters (future)

**Dependencies:** Application, Infrastructure

## Multi-Tenant Architecture

### Data Model

```
User (alice@gmail.com)
  ├── TenantMembership → Tenant: "Acme Research", Role: "Admin"
  └── TenantMembership → Tenant: "Global Study", Role: "Member"

User (bob@example.com)
  └── TenantMembership → Tenant: "Acme Research", Role: "Member"
```

### Key Entities

**User:**
- Represents a person who uses the application
- Can authenticate via social login (Google, GitHub, Microsoft)
- Stored in YOUR database with authentication metadata

**Tenant:**
- Represents a customer organization (just a database record)
- No Azure provisioning needed
- Simple INSERT INTO Tenants (Name, ...) VALUES (...)

**TenantMembership:**
- Links users to tenants with roles
- Roles are stored HERE (not in external identity providers)
- Allows same user to have different roles in different tenants

## Authentication & Authorization

### Where Data Lives

| Data | Storage | Purpose |
|------|---------|---------|
| User identity | OAuth provider (Google/GitHub/etc) | Authentication |
| User record | YOUR database | Application data |
| Roles | YOUR database (TenantMembership) | Authorization |
| Tenant | YOUR database | Customer organization |

### Why This Design?

1. **Multi-tenancy:** Same user can have different roles in different tenants
2. **Control:** You define role semantics (Admin, Moderator, Member)
3. **Flexibility:** Support multiple OAuth providers
4. **Simplicity:** No complex identity infrastructure to manage

## Testing Structure

```
tests/
├── Core.Tests/              # Unit tests for domain logic
├── Application.Tests/       # Unit tests for services
└── Integration.Tests/       # Integration tests for APIs
```

## Build & Run

```bash
# Build entire solution
dotnet build

# Run API
cd src/Api
dotnet run

# Run tests
dotnet test

# Run specific test project
dotnet test tests/Core.Tests
```

## Documentation Structure

```
docs/
├── GETTING-STARTED.md                 # Start here
├── PROJECT-STRUCTURE.md               # This file
├── contexts/                          # Domain and architecture contexts
│   ├── domain-model.md
│   ├── backend-architecture.md
│   ├── security-model.md
│   └── tenant-management.md
├── implementation/                    # Implementation guides
│   ├── social-login-setup.md
│   └── phase-0-foundation.md
├── architecture/                      # Architecture decisions
│   └── decisions/
│       ├── 001-use-clean-architecture.md
│       └── ...
└── templates/                         # Project templates
    ├── adr-template.md
    └── design-doc-template.md
```

## Next Steps

1. **Getting Started:** See `GETTING-STARTED.md`
2. **Social Login Setup:** See `implementation/social-login-setup.md`
3. **Domain Model:** See `contexts/domain-model.md`
4. **Backend Architecture:** See `contexts/backend-architecture.md`

