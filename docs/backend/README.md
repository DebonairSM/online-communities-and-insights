# Backend Architecture

## Overview

Modular monolith built with Clean Architecture principles, designed for multi-tenant SaaS with research community focus.

## Architecture Layers

### Core Layer
- Domain entities and business rules
- No external dependencies
- Pure business logic

### Application Layer  
- Use cases and business orchestration
- Depends only on Core
- Services coordinate domain operations

### Infrastructure Layer
- Database access (EF Core)
- External services (Azure APIs)
- Message bus (Service Bus)

### API Layer
- Controllers and HTTP endpoints
- Authentication/authorization middleware
- Request/response models

## Services

**User Service**: Identity, authentication, profiles
**Community Service**: Multi-tenant communities, groups, memberships  
**Engagement Service**: Posts, comments, reactions, feeds
**Research Service**: Surveys, polls, qualitative tasks, interviews
**Analytics Service**: Metrics, insights, reporting
**Notification Service**: Multi-channel delivery
**Moderation Service**: Content quality, safety, workflow
**Insight Service**: Qualitative coding, themes, story building

## Multi-Tenant Strategy

### Data Isolation
- Shared database with `TenantId` column
- Composite primary keys: `(TenantId, Id)`
- Global query filters enforce tenant scoping

```csharp
builder.Entity<Post>().HasQueryFilter(p => p.TenantId == tenantId);
```

### Security
- Row-level security policies in SQL
- Claims-based authorization with tenant context
- All queries automatically filtered by tenant

## Authentication

### Microsoft Entra External ID
- Managed identity service for consumers
- Custom attributes for tenant/role mapping
- API connectors for token enrichment

```json
{
  "extension_TenantId": "tenant-guid",
  "extension_Roles": ["Member", "Admin"]
}
```

## Data Storage

### Azure SQL Database
- Primary data store for structured data
- Composite keys for tenant isolation
- Row-level security policies

### Azure Blob Storage  
- Media files (images, videos, documents)
- Tenant-scoped containers for isolation

### Redis Cache
- Session storage and rate limiting
- Tenant configuration caching

## Event-Driven Architecture

### Azure Service Bus
- Domain events for cross-service communication
- Outbox pattern for reliable publishing
- Dead-letter queues for error handling

```csharp
public record PostCreated(Guid TenantId, Guid PostId, Guid AuthorId);
```

## Key SaaS Decisions

### Tenant Management
- JIT provisioning from Entra External ID
- Feature flags per tenant
- Custom branding and theming

### Scaling
- Horizontal scaling with stateless design
- Async processing for heavy operations
- CQRS for read-heavy scenarios

### Observability
- Structured logging with Serilog
- Application Insights for monitoring
- Distributed tracing with OpenTelemetry

## Infrastructure Services

- **Azure Media Services**: Video transcoding
- **Azure Cognitive Services**: Speech-to-text, sentiment analysis
- **Azure Communication Services**: Email and SMS
- **Azure Service Bus**: Event messaging
