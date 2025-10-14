# Tech Stack & Packages — Insight Community Platform

Multi-tenant SaaS for online research communities built on .NET 8 + Azure.

---

## 1. Solution Topology

### Architectural Patterns

- **Microservices**: API Gateway (YARP or Azure App Gateway) + bounded services per domain
- **Async Messaging**: Event-driven communication via Azure Service Bus (Topics/Subscriptions)
- **CQRS**: Command/Query separation with distinct read/write models
- **Outbox Pattern**: Transactional messaging guarantee (EF Core outbox table + background processor)
- **Idempotency**: Message deduplication keys stored in Redis or SQL
- **Deployment**: Blue/Green via Azure App Service deployment slots

### Service Boundaries

| Service | Responsibility |
|---------|----------------|
| **Identity & Auth** | Entra ID integration, user profiles, tenant membership, role assignment |
| **Community** | Discussion boards, groups, member directory, content moderation queues |
| **Engagement** | Reactions, comments, notifications preferences, activity feeds |
| **Research** | Surveys, diaries, media uploads, task assignments, quotas |
| **Moderation** | Content review workflows, flagging, approval/rejection |
| **Notification** | Push, email, SMS orchestration; template rendering |
| **Analytics & Insights** | Event ingestion, aggregation, Power BI export APIs |
| **Admin** | Tenant provisioning, configuration, theming, billing integration |

### Multi-Tenancy Strategy

- **Application Layer**: Shared services (single deployment per environment)
- **Data Layer**: DB-per-tenant (preferred) for isolation and compliance
  - Alternative: Single shared DB with TenantId discriminator (lower isolation, simpler ops)
- **Connection Brokering**: Middleware resolves tenant from JWT claim/header → selects connection string from Key Vault or config cache
- **Per-Tenant Assets**: Branding (logos, colors, domain), feature flags, locale settings stored in Admin DB + cached in Redis

---

## 2. Backend (.NET)

### Runtime & Framework

- **.NET 8 LTS** (C# 12)
- **ASP.NET Core 8.0**: Minimal APIs (preferred) or MVC Controllers
- **Worker Services**: Background processors using `IHostedService` or `BackgroundService`

### Core Packages

#### Web & API

```
Microsoft.AspNetCore.OpenApi           # OpenAPI 3.0 spec generation
Swashbuckle.AspNetCore                 # Swagger UI
```

#### Validation

```
FluentValidation                       # Fluent validation rules
FluentValidation.AspNetCore            # ASP.NET Core integration
```

#### Mapping

```
Mapster                                # Fast object mapping (no heavy reflection)
Mapster.DependencyInjection            # DI integration
```

#### Resilience

```
Polly                                  # Retry, circuit breaker, timeout policies
Polly.Extensions.Http                  # HttpClient integration
```

#### Caching

```
Microsoft.Extensions.Caching.StackExchangeRedis  # Distributed cache
StackExchange.Redis                                # Direct Redis client
```

#### Rate Limiting

```
AspNetCoreRateLimit                    # Throttling middleware
```
*Alternative*: Built-in .NET 7+ Rate Limiting middleware (System.Threading.RateLimiting)

#### Health Checks

```
AspNetCore.HealthChecks.UI             # Health check dashboard
AspNetCore.HealthChecks.UI.Client      # JSON formatter
AspNetCore.HealthChecks.SqlServer      # SQL Server checks
AspNetCore.HealthChecks.AzureServiceBus # Service Bus checks
AspNetCore.HealthChecks.Uris           # HTTP endpoint checks
AspNetCore.HealthChecks.Redis          # Redis checks
AspNetCore.HealthChecks.CosmosDb       # Cosmos DB checks
```

### Data Access

#### Relational (Primary)

```
Microsoft.EntityFrameworkCore.SqlServer   # Azure SQL provider
Microsoft.EntityFrameworkCore.Relational  # Core abstractions
Microsoft.EntityFrameworkCore.Design      # Migrations tooling
EFCore.BulkExtensions                     # Bulk insert/update/delete
```

#### Read-Optimized Queries (Optional)

```
Dapper                                    # Micro-ORM for read-heavy queries
```

#### NoSQL (Content Streams / Attachments Metadata)

```
Microsoft.Azure.Cosmos                    # Cosmos DB SDK v3
```

### Messaging & Integration

#### Azure Service Bus (Native SDK)

```
Azure.Messaging.ServiceBus                # Official SDK (recommended)
```

#### Message Bus Abstraction (Choose One)

**Option A: MassTransit**
```
MassTransit                               # Framework
MassTransit.Azure.ServiceBus.Core         # Azure Service Bus transport
MassTransit.EntityFrameworkCore           # Outbox support
```

**Option B: Wolverine**
```
Wolverine                                 # Lightweight messaging
Wolverine.AzureServiceBus                 # Azure transport
```
- Built-in outbox, no extra packages

**Option C: Brighter**
```
Brighter                                  # Command processor
Brighter.MessagingGateway.AzureServiceBus # Transport
```

**Decision**: Use **Azure.Messaging.ServiceBus** directly + custom outbox for maximum control and minimal abstraction.

### Storage & Media

```
Azure.Storage.Blobs                       # Blob storage (images, videos, files)
Azure.Data.Tables                         # Table storage (if needed for simple key-value)
Azure.Storage.Queues                      # Storage queues (if needed for fire-and-forget tasks)
```

Infra: Azure Front Door or Azure CDN for blob delivery

### Security & Identity

#### Authentication

**Phase 2 (Current):** OAuth 2.0 Social Login
```
Microsoft.AspNetCore.Authentication.JwtBearer      # JWT validation for API access
Microsoft.AspNetCore.Authentication.Google         # Google OAuth
AspNet.Security.OAuth.GitHub                       # GitHub OAuth
Microsoft.AspNetCore.Authentication.MicrosoftAccount # Microsoft personal accounts
Microsoft.IdentityModel.Tokens                     # Token handling
System.IdentityModel.Tokens.Jwt                    # JWT library
```

**Phase 3 (Future):** Enterprise SSO
```
Microsoft.Identity.Web                             # Microsoft Entra ID integration
Microsoft.Identity.Web.MicrosoftGraph             # Graph API client (if user sync needed)
```

#### Secrets Management

```
Azure.Security.KeyVault.Secrets          # Key Vault secrets
Azure.Security.KeyVault.Certificates     # Key Vault certs
Azure.Identity                           # Managed identity authentication
```

#### Feature Flags

```
Microsoft.FeatureManagement.AspNetCore   # Feature toggles
```

### Observability

#### Structured Logging

```
Serilog.AspNetCore                       # Serilog integration
Serilog.Sinks.Console                    # Console output
Serilog.Sinks.Seq                        # Seq log aggregator (dev/test)
Serilog.Sinks.ApplicationInsights        # App Insights sink (prod)
Serilog.Enrichers.Environment            # Machine/env enrichers
Serilog.Exceptions                       # Exception enrichment
Serilog.Enrichers.Span                   # Activity/span enrichment
```

#### Tracing & Metrics

```
OpenTelemetry.Extensions.Hosting         # OTEL host integration
OpenTelemetry.Instrumentation.AspNetCore # ASP.NET Core traces
OpenTelemetry.Instrumentation.Http       # HttpClient traces
OpenTelemetry.Instrumentation.SqlClient  # SQL traces
OpenTelemetry.Exporter.OpenTelemetryProtocol # OTLP exporter
Azure.Monitor.OpenTelemetry.Exporter     # App Insights exporter
```

Correlation: W3C `traceparent` header propagated via OpenTelemetry; enrich Serilog with `Activity.Current.TraceId`

### File Processing & Media

```
SixLabors.ImageSharp                     # Image resizing, format conversion
```

Video transcoding: ffmpeg in worker container (Azure Container Instances or AKS Job)

### Testing

#### Unit Tests

```
xunit                                    # Test framework
xunit.runner.visualstudio                # VS integration
FluentAssertions                         # Readable assertions
```

#### Mocking

```
Moq                                      # Mocking library
```
*Alternative*: NSubstitute

#### Integration/Contract Tests

```
Microsoft.AspNetCore.Mvc.Testing         # WebApplicationFactory
Testcontainers                           # Docker containers for tests (SQL, Redis)
WireMock.Net                             # HTTP mocking
Verify.Xunit                             # Snapshot testing
```

---

## 3. Frontend (Web)

### Stack

- **Framework**: React 18 + TypeScript 5
- **Build Tool**: Vite 5 (preferred) or Next.js 14 (if SSR/SSG needed)
- **Styling**: TailwindCSS 3
- **Component Library**: shadcn/ui (Radix UI primitives + Tailwind)

### Core Packages

#### State Management

**Option A: Redux**
```bash
@reduxjs/toolkit
react-redux
```

**Option B: React Query (Recommended for API state)**
```bash
@tanstack/react-query
@tanstack/react-query-devtools
```

#### Forms & Validation

```bash
react-hook-form            # Form state management
zod                        # TypeScript-first schema validation
@hookform/resolvers        # Zod resolver for react-hook-form
```

#### Routing

**Vite**
```bash
react-router-dom
```

**Next.js**: Built-in App Router

#### UI & Visualization

```bash
recharts                   # Charts (responsive, composable)
lucide-react               # Icon library
cmdk                       # Command palette (if needed)
date-fns                   # Date utilities
```

#### Internationalization & Accessibility

```bash
react-i18next
i18next
i18next-browser-languagedetector
```

ARIA: Use semantic HTML + Radix UI primitives (via shadcn/ui) for built-in a11y

#### Authentication

**Phase 2 (Current):** OAuth 2.0 social login handled by backend, frontend receives JWT

**Phase 3 (Future):** Enterprise SSO with Microsoft Entra ID
```bash
@azure/msal-browser        # MSAL.js for Microsoft Entra ID (work accounts)
@azure/msal-react          # React hooks/HOCs
```

#### Rich Text Editing

```bash
@tiptap/react              # Headless editor
@tiptap/starter-kit        # Common extensions
@tiptap/extension-link
@tiptap/extension-image
```

### Build & Quality Tools

```bash
eslint                     # Linting
@typescript-eslint/parser
@typescript-eslint/eslint-plugin
prettier                   # Code formatting
eslint-config-prettier     # Disable ESLint formatting rules
vitest                     # Unit testing (Vite native)
@testing-library/react     # Component testing
@testing-library/jest-dom
@testing-library/user-event
playwright                 # E2E testing
```

---

## 4. Mobile

### Option A: React Native (Expo)

- **Framework**: Expo SDK 50+
- **Shared Code**: Auth logic, API clients, business rules reused from web
- **Navigation**: `@react-navigation/native`
- **UI**: React Native Paper or NativeBase
- **Offline**: `@react-native-async-storage/async-storage` + background sync

### Option B: Progressive Web App (PWA)

- **Service Workers**: Vite PWA plugin (`vite-plugin-pwa`)
- **Offline**: Cache API + IndexedDB
- **Background Sync**: Background Sync API for media uploads
- **Install**: Add to Home Screen prompt

### Push Notifications

- **Infrastructure**: Azure Notification Hubs
- **Client SDKs**:
  - iOS: APNs via Notification Hubs SDK
  - Android: FCM via Notification Hubs SDK
  - Web: Web Push API + service worker

**Recommendation**: Start with PWA for MVP; add React Native if native features (camera, file system) are required.

---

## 5. Analytics & Insights Layer

### Event Capture

- **Schema**: Append-only events to SQL table (`EventStore`) or Blob storage (JSON-ND)
- **Ingestion**: Service Bus Topic → Worker Service writes to store
- **Partitioning**: By `TenantId` + `EventDate` for query performance

### Processing

- **Compute**: Azure Functions (Isolated) or Worker Services (long-running aggregators)
- **Windowing**: Tumbling/sliding windows via Reactive Extensions (Rx.NET) or custom logic
- **Triggers**: Timer (cron), Service Bus (event-driven)

### Data Warehousing

**Option A: Azure Synapse Analytics Serverless**
- Query JSON/Parquet files in Data Lake (Blob storage)
- External tables + OPENROWSET for ad-hoc SQL

**Option B: Azure SQL + Partitioning**
- Partitioned fact tables by `TenantId` + `Month`
- Indexed views for common aggregations

### Dashboarding

- **Primary**: Power BI Embedded
  - Export APIs for tenant-level datasets
  - Row-level security (RLS) filters by `TenantId`
- **Alternative**: Custom dashboards in React (recharts + @tanstack/react-query)

### AI / NLP (Optional)

| Service | Use Case | SDK |
|---------|----------|-----|
| Azure AI Speech | Audio transcription (diaries, video uploads) | Azure.AI.OpenAI or REST |
| Azure AI Language | Sentiment analysis, key phrase extraction | Azure.AI.TextAnalytics |
| Azure OpenAI | Clustering, summarization, theme extraction | Azure.AI.OpenAI |

---

## 6. DevOps & Environments

### Repository Structure

- **Mono-repo** (recommended): Single repo with `/src/services/`, `/src/web/`, `/infrastructure/`
- **Branching**: Trunk-based development; feature branches via PRs
- **Commit Conventions**: Conventional Commits (feat, fix, chore)

### CI/CD Pipelines

**GitHub Actions** (preferred) or Azure DevOps Pipelines

#### Build Pipeline
1. Restore dependencies
2. Build solution
3. Run unit tests
4. Static analysis (CodeQL, Snyk)
5. Build Docker images → push to Azure Container Registry
6. Publish artifacts (binaries, IaC templates)

#### Deployment Pipeline
1. Deploy IaC (Bicep → Azure)
2. Run migrations (EF Core `dotnet ef database update`)
3. Deploy API services (App Service slots or AKS rolling update)
4. Deploy web app (App Service or Static Web Apps)
5. Smoke tests (health checks, E2E critical paths)
6. Swap slots (blue/green) or promote deployment

### Infrastructure as Code

**Bicep** (preferred): Azure-native, concise syntax
```
infrastructure/
  main.bicep
  modules/
    app-service.bicep
    sql-database.bicep
    service-bus.bicep
    key-vault.bicep
```

**Alternative**: Terraform (if multi-cloud or existing TF expertise)

### Containerization

- **Base Images**: `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime), `mcr.microsoft.com/dotnet/sdk:8.0` (build)
- **Multi-stage Builds**: Build → Publish → Runtime
- **Orchestration**: Azure Kubernetes Service (AKS) or App Service (Linux containers with deployment slots)

### Secrets Management

- **Source**: Azure Key Vault
- **Access**: Managed Identity (system-assigned or user-assigned)
- **Injection**: Key Vault references in App Service config or CSI driver in AKS

### Quality Gates

- **SAST**: GitHub CodeQL (C#, TypeScript, SQL)
- **Dependency Scanning**: Dependabot, Snyk, or GitHub Advanced Security (GHAS)
- **License Compliance**: Whitesource Bolt or FOSSA
- **Code Coverage**: Coverlet + CodeCov; enforce 80%+ for business logic

---

## 7. Azure Services

### Compute

| Service | Use Case |
|---------|----------|
| **Azure App Service** (Linux, P1v3+) | API services, admin portal, web app (with deployment slots) |
| **Azure Kubernetes Service (AKS)** | Alternative for microservices at scale; sidecar patterns (Dapr optional) |
| **Azure Functions** (Isolated) | Event processing, scheduled aggregations, webhook handlers |
| **Azure Container Instances** | One-off jobs (video transcoding, bulk imports) |

### Data & Storage

| Service | Use Case |
|---------|----------|
| **Azure SQL Database** (Hyperscale or GP) | Primary RDBMS; per-tenant DBs or shared with discriminator |
| **Azure Cosmos DB** (NoSQL, partition by `TenantId`) | Activity feeds, unstructured content metadata |
| **Azure Blob Storage** (Hot/Cool tiers) | Media files (images, videos, documents); CDN origin |
| **Azure Cache for Redis** (Standard C1+) | Distributed cache, session state, rate-limit counters |
| **Azure Data Lake Storage Gen2** | Event archive, analytics source (Parquet/JSON-ND) |

### Messaging & Integration

| Service | Use Case |
|---------|----------|
| **Azure Service Bus** (Standard or Premium) | Topics/subscriptions for inter-service events, FIFO queues |
| **Azure Event Grid** | Webhook delivery (external integrations), blob upload events |

### Networking & Security

| Service | Use Case |
|---------|----------|
| **Azure Application Gateway + WAF** | Ingress, SSL termination, OWASP protection |
| **Azure Front Door** | Global CDN, DDoS protection, geo-routing |
| **Azure Private Endpoints** | VNet-integrated services (SQL, Storage, Key Vault) |
| **Azure Virtual Network** | Service isolation, NSGs, peering to on-prem (if needed) |

### Identity & Access

| Service | Use Case |
|---------|----------|
| **Microsoft Entra ID** (Azure AD) | User authentication, B2B guest access |
| **App Registrations** | OAuth2/OIDC clients (web, mobile, SPA) |
| **Managed Identities** | Service-to-service auth (Key Vault, SQL, Service Bus) |

### Monitoring & Operations

| Service | Use Case |
|---------|----------|
| **Application Insights** | APM, distributed tracing, live metrics |
| **Log Analytics Workspace** | Centralized logs, KQL queries |
| **Azure Monitor** | Alerts (metric/log-based), action groups (email, webhook, Logic App) |
| **Azure Monitor Workbooks** | Custom operational dashboards |

### Notifications

| Service | Use Case |
|---------|----------|
| **Azure Notification Hubs** | Push notifications (iOS, Android, Web Push) |
| **Azure Communication Services** | Transactional email and SMS |

---

## 8. Security & Compliance

### Authentication & Authorization

- **Phase 2**: OAuth 2.0 social login (Google, GitHub, Microsoft personal accounts) with self-issued JWT tokens
- **Phase 3 (Future)**: Microsoft Entra ID for enterprise SSO (work accounts)
- **RBAC**: Role claims in JWT; platform roles (Admin, Moderator, Member) + tenant-specific roles stored in YOUR database
- **Least Privilege**: Managed identities for service-to-service; no connection strings in code

### Data Protection

- **TLS 1.2+**: All transport encrypted; enforce HSTS headers
- **Encryption at Rest**: SQL TDE, Blob storage encryption (default), Redis encryption
- **PII Controls**: Field-level encryption for sensitive data (AES-256); Key Vault for keys
- **Consent Logging**: Audit table for GDPR/CCPA consent events (timestamps, IP, user)

### OWASP Top 10 Mitigations

| Threat | Mitigation |
|--------|------------|
| Injection | Parameterized queries (EF Core, Dapper); input validation (FluentValidation) |
| Broken Auth | Entra ID tokens (short-lived); refresh token rotation |
| Sensitive Data Exposure | Key Vault; no secrets in config/logs; redact PII in Serilog |
| XXE | Disable external entity processing in XML parsers |
| Broken Access Control | Policy-based auth; validate `TenantId` in every request |
| Security Misconfiguration | IaC with secure defaults; disable debug mode in prod |
| XSS | React auto-escaping; CSP headers; sanitize rich text (DOMPurify) |
| Insecure Deserialization | Avoid BinaryFormatter; use System.Text.Json with type restrictions |
| Using Components with Known Vulnerabilities | Dependabot + Snyk scans; auto-update minor/patch versions |
| Insufficient Logging | Structured logs to Log Analytics; alert on auth failures |

### Retention & Purge

- **Audit Logs**: 7 years in Log Analytics + cold storage (Blob Archive tier)
- **User Content**: Per-tenant retention policy; scheduled job deletes soft-deleted records after 30 days
- **PII**: Right-to-erasure API deletes user data across all tenants; log redaction

---

## 9. Package Installation Matrix

### Backend: API Service

```bash
# Web & API
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Swashbuckle.AspNetCore

# Validation
dotnet add package FluentValidation
dotnet add package FluentValidation.AspNetCore

# Mapping
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection

# Resilience
dotnet add package Polly
dotnet add package Polly.Extensions.Http

# Caching
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package StackExchange.Redis

# Rate Limiting
dotnet add package AspNetCoreRateLimit

# Health Checks
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.Client
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.AzureServiceBus
dotnet add package AspNetCore.HealthChecks.Uris
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.CosmosDb

# Data Access
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Relational
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package EFCore.BulkExtensions
dotnet add package Dapper
dotnet add package Microsoft.Azure.Cosmos

# Messaging
dotnet add package Azure.Messaging.ServiceBus

# Storage
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Data.Tables

# Security
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
dotnet add package Microsoft.FeatureManagement.AspNetCore

# Observability
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Exceptions
dotnet add package Serilog.Enrichers.Span
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.SqlClient
dotnet add package Azure.Monitor.OpenTelemetry.Exporter

# Media Processing
dotnet add package SixLabors.ImageSharp
```

### Backend: Worker Service

```bash
# Base
dotnet add package Microsoft.Extensions.Hosting

# Messaging
dotnet add package Azure.Messaging.ServiceBus

# Data Access
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.Azure.Cosmos

# Storage
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Storage.Queues

# Security
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity

# Observability (same as API)
dotnet add package Serilog.Extensions.Hosting
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package Azure.Monitor.OpenTelemetry.Exporter
```

### Backend: Shared Library (Domain/DTOs)

```bash
dotnet add package Mapster
dotnet add package FluentValidation
```

### Backend: Testing Project

```bash
# Unit Testing
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package FluentAssertions
dotnet add package Moq

# Integration Testing
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package Testcontainers.MsSql
dotnet add package Testcontainers.Redis
dotnet add package WireMock.Net
dotnet add package Verify.Xunit
```

### Frontend: Web App (React + Vite)

```bash
# Core
npm install react react-dom
npm install -D typescript @types/react @types/react-dom

# Build Tool
npm install -D vite @vitejs/plugin-react

# Routing
npm install react-router-dom

# State Management
npm install @tanstack/react-query @tanstack/react-query-devtools

# Forms & Validation
npm install react-hook-form zod @hookform/resolvers

# Styling
npm install -D tailwindcss postcss autoprefixer
npm install tailwindcss-animate class-variance-authority clsx tailwind-merge

# UI Components (shadcn/ui - installed via CLI, then:)
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-label @radix-ui/react-select @radix-ui/react-toast

# Charts
npm install recharts

# Icons & Utilities
npm install lucide-react
npm install date-fns

# i18n
npm install react-i18next i18next i18next-browser-languagedetector

# Auth
npm install @azure/msal-browser @azure/msal-react

# Rich Text
npm install @tiptap/react @tiptap/starter-kit @tiptap/extension-link @tiptap/extension-image

# Linting & Formatting
npm install -D eslint @typescript-eslint/parser @typescript-eslint/eslint-plugin
npm install -D prettier eslint-config-prettier

# Testing
npm install -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event
npm install -D @playwright/test
```

### Mobile: React Native (Expo)

```bash
# Expo CLI
npx create-expo-app@latest

# Navigation
npx expo install @react-navigation/native @react-navigation/native-stack
npx expo install react-native-screens react-native-safe-area-context

# Auth (MSAL for React Native)
npm install @azure/msal-react-native
npx expo install expo-secure-store

# Storage
npx expo install @react-native-async-storage/async-storage

# Notifications
npx expo install expo-notifications
```

---

## 10. Non-Goals & Swaps

### Explicitly Excluded

| Technology | Reason |
|------------|--------|
| **Dapr** | Adds abstraction layer; direct Azure SDK usage preferred for transparency |
| **NHibernate** | EF Core is standard for .NET; team familiarity |
| **GraphQL** | REST + OData (if needed) sufficient; avoids N+1 query complexity |
| **SignalR** | Server-sent events (SSE) or polling for real-time updates; simpler deployment |
| **Azure API Management** | App Gateway + YARP or service-to-service direct calls; APIM reserved for external partner APIs |

### Accepted Swaps

| Decision Point | Choice A | Choice B | Reasoning |
|----------------|----------|----------|-----------|
| **Message Bus** | Azure.Messaging.ServiceBus (native) | MassTransit | Native SDK = less abstraction, lower ceremony; MassTransit if saga/routing needed |
| **API Style** | Minimal APIs | MVC Controllers | Minimal APIs for new services (less boilerplate); Controllers if complex model binding |
| **Frontend Framework** | Vite + React | Next.js | Vite for SPA (faster dev); Next.js if SSR/SSG required for SEO/performance |
| **Tenancy** | DB-per-tenant | Shared DB + discriminator | DB-per-tenant = better isolation/compliance; shared DB = lower cost/simpler ops |
| **Hosting** | App Service + Slots | AKS | App Service = lower ops overhead; AKS if need sidecars, mesh, or multi-region active-active |
| **IaC** | Bicep | Terraform | Bicep = Azure-native, simpler; Terraform if multi-cloud or existing investment |

---

## Revision History

| Date | Change |
|------|--------|
| 2025-10-11 | Initial version |

