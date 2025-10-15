# Architecture Decisions

## ADR-001: Clean Architecture with Modular Monolith

**Decision**: Use Clean Architecture organized as modular monolith for clear separation of concerns and future microservice evolution capability.

**Key Points**:
- Business logic in Core layer (no dependencies)
- Application layer orchestrates use cases  
- Infrastructure implements Core interfaces
- API layer handles HTTP concerns

```csharp
src/
├── Core/           # Domain entities, interfaces
├── Application/    # Business logic, use cases  
├── Infrastructure/ # Data access, external services
└── Api/           # Controllers, middleware
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

## ADR-007: Self-Issued JWT Tokens

**Decision**: Always generate our own JWT tokens regardless of authentication provider.

**Rationale**: Unified token format, application-specific claims (tenant, roles), consistent validation.
