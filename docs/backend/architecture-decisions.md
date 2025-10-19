# Architecture Decisions

## ADR-001: Clean Architecture with Modular Monolith

**Decision**: Use Clean Architecture organized as modular monolith for clear separation of concerns and future microservice evolution capability.

**Key Points**:
- Business logic in Core layer (no dependencies)
- Application layer orchestrates use cases  
- Infrastructure implements Core interfaces
- API layer handles HTTP concerns

```csharp
backend/src/
├── Core/           # Domain entities, interfaces
├── Application/    # Business logic, use cases  
├── Infrastructure/ # Data access, external services
└── Api/            # Controllers, middleware
```

## ADR-002: Azure Media Services

**Decision**: Use Azure Media Services for video processing instead of custom FFmpeg pipeline.

**Rationale**: Managed service handles encoding, thumbnails, streaming automatically with Azure integration.

## ADR-003: Azure Communication Services

**Decision**: Use Azure Communication Services for email instead of SendGrid.

**Rationale**: Native Azure integration, pay-per-use pricing, unified billing and monitoring.

## ADR-004: Azure Cognitive Services

**Decision**: Use Azure Cognitive Services Speech-to-Text for transcription.

**Rationale**: $1/hour pricing, native Azure integration, supports speaker diarization and custom models.

## ADR-005: Composite Keys for Multi-Tenancy

**Decision**: Use shared database with composite primary keys `(TenantId, Id)` for tenant isolation.

**Implementation**:
```sql
CREATE TABLE Communities (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_Communities PRIMARY KEY (TenantId, Id)
);
```

## ADR-006: Authentication Strategy

**Decision**: Use Microsoft Entra External ID for managed authentication with custom tenant/role claims.

**Benefits**: MFA support, breach protection, compliance features, reduced maintenance.

## ADR-007: Microsoft Entra External ID Token Validation

**Decision**: Use Microsoft Entra External ID issued JWT tokens exclusively for authentication.

**Rationale**: Enterprise-grade security, compliance certifications (SOC 2, FedRAMP, ISO 27001), managed token lifecycle, and built-in threat protection. Custom claims (tenant, roles) are enriched via API Connector during token issuance.

## ADR-008: Migration from OAuth Social Login to Entra External ID Only

**Decision**: Remove OAuth 2.0 social login implementation and use only Microsoft Entra External ID for authentication.

**Context**: Originally planned to support Google, GitHub, and Microsoft Personal Account OAuth providers alongside Entra External ID.

**Decision**: Use only Microsoft Entra External ID for all authentication needs.

**Rationale**: 
- **Compliance**: Entra External ID provides SOC 2, FedRAMP, and ISO 27001 compliance out of the box
- **Security**: Managed threat protection, MFA support, and breach detection
- **Simplification**: Reduces codebase complexity and maintenance overhead
- **Enterprise Focus**: Aligns with target market of enterprise customers
- **Custom Claims**: Better support for tenant and role management through API Connectors

**Implementation**:
- Removed ExternalAuthService and OAuth callback endpoints
- Removed self-issued JWT token generation
- Configured JWT Bearer authentication to validate only Entra External ID tokens
- Updated ClaimsPrincipalExtensions to work only with Entra External ID claim format

**Removed Components**:
- `ExternalAuthService.cs` and `IExternalAuthService.cs`
- OAuth callback endpoints in `AuthController.cs`
- Self-issued JWT configuration and secret key management
- OAuth provider configurations in `appsettings.json`
