# Backend Architecture

## Domain-Driven Service Decomposition

The backend is organized as a modular monolith with clear bounded contexts. Services communicate via direct API calls within the monolith and via message bus for cross-cutting concerns. Future evolution to microservices is possible by extracting services into separate deployments.

### User Service

**Responsibility**: User identity, authentication, profile management, and authorization.

**Core Entities**:
- `User`: Identity, credentials, profile data
- `Role`: System and tenant-specific roles
- `Permission`: Granular permissions for RBAC
- `UserClaim`: Custom claims for policy-based authorization

**Key Operations**:
- User registration and activation
- Authentication (local + SSO)
- Password reset and email verification
- Profile CRUD operations
- Role and permission assignment
- User search and directory listings

**Data Store**: Azure SQL Database
**Events Published**: `UserRegistered`, `UserActivated`, `UserDeactivated`, `ProfileUpdated`

### Community Service

**Responsibility**: Community and group management, membership, and access control.

**Core Entities**:
- `Tenant`: Top-level organization (client)
- `Community`: Branded space within a tenant
- `Group`: Sub-communities or segments
- `Membership`: User participation in communities/groups
- `CommunitySettings`: Configuration and branding

**Key Operations**:
- Community creation and configuration
- Group management within communities
- Membership operations (join, leave, invite)
- Access control policies
- Branding and theme management

**Data Store**: Azure SQL Database
**Events Published**: `CommunityCreated`, `MemberJoined`, `MemberLeft`, `GroupCreated`

### Engagement Service

**Responsibility**: Content creation, interactions, and social features.

**Core Entities**:
- `Post`: User-generated content (text, images, video)
- `Comment`: Threaded responses to posts
- `Reaction`: Likes, upvotes, sentiment indicators
- `Mention`: User mentions in content
- `ContentFeed`: Aggregated view of content

**Key Operations**:
- Post and comment CRUD
- Reaction management
- Feed generation (algorithmic or chronological)
- Content moderation workflows
- Media upload and processing
- Search and filtering

**Data Store**: 
- Azure SQL Database for structured data
- Azure Cosmos DB for high-volume activity streams
- Azure Blob Storage for media

**Events Published**: `PostCreated`, `CommentAdded`, `ReactionAdded`, `ContentFlagged`

### Research Engine Service

**Responsibility**: Comprehensive research activity management including quantitative and qualitative methods.

**Core Entities**:
- `Survey`: Multi-question research instrument with logic/branching
- `Question`: Survey questions (text, choice, scale, matrix, MaxDiff, conjoint)
- `SurveyResponse`: Participant responses with timestamps
- `Poll`: Simple voting mechanism
- `ResearchTask`: Qualitative activities (diary, photo, video, collage)
- `TaskSubmission`: Participant media and text submissions for tasks
- `Interview`: In-depth interview scheduling and metadata
- `InterviewRecording`: Audio/video recordings with transcripts
- `FocusGroup`: Virtual group session details
- `FocusGroupParticipant`: Attendance and participation tracking
- `MediaAnnotation`: Markup and tags on images/videos
- `Quota`: Sampling rules and participant selection criteria

**Key Operations**:
- **Quantitative**: Survey builder, logic/branching, response validation, statistical aggregation
- **Qualitative**: Diary prompts, photo/video upload, annotation tools, collaging interface
- **IDI Management**: Schedule interviews, capture consent, record sessions, auto-transcribe
- **Focus Groups**: Create virtual sessions, invite participants, record, export chat logs
- **Quota Enforcement**: Automated participant selection based on demographics/behavior
- **Task Lifecycle**: Create, assign, remind, track completion, approve/reject submissions
- **Media Processing**: Transcode videos, generate thumbnails, extract metadata

**Data Store**: 
- Azure SQL Database for structured research data
- Azure Blob Storage for participant media
- Azure Media Services for video transcoding

**Events Published**: `SurveyPublished`, `ResponseSubmitted`, `TaskCompleted`, `InterviewScheduled`, `FocusGroupRecorded`, `QuotaFilled`

**Integration Points**:
- Azure Cognitive Services for auto-transcription and sentiment analysis
- Azure Media Services for video processing
- External transcription services (Rev, Otter.ai) via webhooks

### Analytics Service

**Responsibility**: Data aggregation, metrics calculation, and insight generation.

**Core Entities**:
- `EngagementMetric`: Pre-calculated engagement KPIs
- `MemberSegment`: Cohorts based on behavior
- `Report`: Scheduled or on-demand reports
- `Dashboard`: Custom analytics views

**Key Operations**:
- Real-time metric calculation
- Batch aggregation jobs
- Sentiment analysis (Azure Cognitive Services)
- Trend detection
- Member segmentation
- Report generation and scheduling
- Data export for external BI

**Data Store**: 
- Azure SQL Database for aggregated metrics
- Azure Synapse Analytics or dedicated data warehouse (optional for large tenants)

**Events Consumed**: All domain events for analytics processing
**Events Published**: `ReportGenerated`, `AlertTriggered`

### Notification Service

**Responsibility**: Multi-channel notification delivery and preference management.

**Core Entities**:
- `Notification`: Individual notification record
- `NotificationTemplate`: Templated messages
- `NotificationPreference`: User preferences per channel
- `NotificationDelivery`: Delivery status tracking

**Key Operations**:
- Send notifications (in-app, email, SMS, push)
- Template rendering with personalization
- Delivery tracking and retry logic
- Preference management
- Digest/batching for high-volume notifications

**Data Store**: 
- Azure SQL Database for preferences and delivery logs
- Azure Cosmos DB for high-volume notification queue

**Integration**: 
- Azure Communication Services for email and SMS
- Firebase Cloud Messaging or APNs for push

**Events Consumed**: All events that trigger notifications
**Events Published**: `NotificationSent`, `NotificationFailed`

### Moderation Service

**Responsibility**: Content quality control, safety enforcement, and human moderation workflows.

**Core Entities**:
- `ModerationQueue`: Pending content awaiting review
- `ModerationRule`: Automated flagging criteria (profanity, PII, spam)
- `ModerationAction`: Approve, reject, request revision, escalate
- `QualityScore`: Participant contribution quality metrics
- `SafetyReport`: Member-reported violations
- `ModeratorNote`: Internal annotations on contributions

**Key Operations**:
- **Automated Flagging**: Detect profanity, PII (emails, phone numbers), spam patterns
- **Manual Review**: Queue flagged content for moderator approval/rejection
- **Quality Scoring**: Rate participant contributions on depth, relevance, authenticity
- **Facilitation**: Moderators prompt for more detail, ask follow-up questions
- **Safety Enforcement**: Block participants, hide content, escalate to admin
- **Workflow Management**: Assign content to moderators, track review time, SLA monitoring

**Data Store**: Azure SQL Database

**Events Published**: `ContentFlagged`, `ContentApproved`, `ContentRejected`, `ParticipantWarned`, `ParticipantBlocked`

**Integration Points**:
- Azure Content Moderator for automated profanity/PII detection
- Custom ML models for content quality scoring

### Insight Workspace Service

**Responsibility**: Qualitative analysis tools for researchers to code, theme, and compile insights.

**Core Entities**:
- `Theme`: Qualitative coding category (e.g., "Price Sensitivity", "Brand Loyalty")
- `Code`: Specific tags within themes
- `CodedSegment`: Responses or media clips tagged with codes
- `Quote`: Selected participant quotes with attribution/anonymization
- `InsightStory`: Narrative deliverable combining quotes, media, and stats
- `StorySection`: Chapters in insight story (Executive Summary, Findings, Recommendations)
- `CodingTaxonomy`: Hierarchical organization of themes and codes

**Key Operations**:
- **Qualitative Coding**: Tag responses with themes, support multiple coders, calculate inter-coder reliability
- **Theme Management**: Create, merge, nest themes; import from previous studies
- **Quote Selection**: Mark impactful quotes, attribute or anonymize, organize by theme
- **Sentiment Tagging**: Manual or AI-assisted sentiment coding (positive, negative, neutral, mixed)
- **Insight Story Builder**: Drag-and-drop interface to compile narrative with quotes, charts, media clips
- **Collaboration**: Multiple analysts can code simultaneously, resolve conflicts, leave comments
- **Export**: Generate PowerPoint, PDF, Word reports with branding

**Data Store**: Azure SQL Database

**Events Published**: `ThemeCreated`, `ResponseCoded`, `InsightStoryPublished`

**Integration Points**:
- Azure Cognitive Services for sentiment analysis suggestions
- Office 365 APIs for PowerPoint/Word generation
- MaxQDA/NVivo export formats

### Incentive Service

**Responsibility**: Participant reward management, points tracking, and fulfillment.

**Core Entities**:
- `IncentiveAccount`: Participant points balance
- `Transaction`: Points earned or redeemed
- `Reward`: Gift cards, products, sweepstakes entries
- `RedemptionRequest`: Participant claim for reward
- `IncentiveBudget`: Per-community budget tracking

**Key Operations**:
- **Points Accumulation**: Award points for survey completion, diary submission, discussion participation
- **Tiered Rewards**: Different point values for different activity types and quality levels
- **Reward Catalog**: Maintain list of available rewards (gift cards, merch, donations)
- **Redemption**: Process participant requests, fulfill via third-party platforms (Tremendous, Rybbon)
- **Budget Management**: Track spend per community, alert when approaching limit
- **Fraud Prevention**: Detect suspicious patterns (multiple accounts, bot responses)

**Data Store**: Azure SQL Database

**Events Published**: `PointsAwarded`, `RewardRedeemed`, `BudgetAlertTriggered`

**Integration Points**:
- Tremendous, Rybbon, or Tango Card for digital gift card fulfillment
- Stripe for direct payments to participants

### Consent Service

**Responsibility**: Research ethics compliance, participant consent management, and data rights.

**Core Entities**:
- `ConsentForm`: Study-specific consent document (IRB-approved text)
- `ConsentRecord`: Participant agreement to specific study with timestamp
- `DataRightsRequest`: GDPR access, erasure, or portability requests
- `ParticipantWithdrawal`: Exit from study with data handling preferences

**Key Operations**:
- **Consent Capture**: Present consent forms before study activities, require explicit agreement
- **Consent Versioning**: Track changes to consent language, re-consent when terms change
- **Audit Trail**: Immutable log of all consent events (who, what, when, where, IP address)
- **Data Rights**: Process participant requests for data access, deletion, portability
- **Withdrawal Management**: Handle study exits, anonymize or delete data per participant choice
- **Minor Protection**: Age verification, parental consent workflows for under-18 research

**Data Store**: Azure SQL Database (append-only consent logs)

**Events Published**: `ConsentGranted`, `ConsentWithdrawn`, `DataRightsRequestReceived`, `DataErased`

**Integration Points**:
- Electronic signature platforms (DocuSign) for high-stakes research
- GDPR compliance tools

### Admin Service

**Responsibility**: Platform administration, tenant management, and operations.

**Core Entities**:
- `TenantConfiguration`: Per-tenant settings
- `FeatureFlag`: Toggle features per tenant or globally
- `AuditLog`: Immutable audit trail
- `BillingAccount`: Subscription and billing info
- `UsageMetric`: Resource consumption tracking

**Key Operations**:
- Tenant provisioning and deactivation
- Feature flag management
- Audit log queries
- Billing integration
- System health monitoring
- User impersonation (with audit trail)

**Data Store**: Azure SQL Database
**Events Published**: `TenantProvisioned`, `FeatureFlagChanged`, `BillingEventRecorded`

## API Strategy

### REST APIs

Primary API pattern using ASP.NET Core Web API with OpenAPI 3.0 documentation.

**Design Principles**:
- Resource-oriented URLs (`/api/v1/communities/{id}/posts`)
- HTTP verbs for operations (GET, POST, PUT, PATCH, DELETE)
- JSON request/response bodies
- HATEOAS links for discoverability (optional)
- Consistent error responses with problem details (RFC 7807)

**Versioning**:
- URL-based versioning (`/api/v1/`, `/api/v2/`)
- Maintain backward compatibility within major versions
- Deprecation notices via response headers

**Rate Limiting**:
- Per-tenant quotas based on subscription tier
- Per-user rate limits to prevent abuse
- 429 Too Many Requests with Retry-After header

### GraphQL (Future)

Planned GraphQL layer for flexible client queries and reduced over-fetching.

**Use Cases**:
- Complex UI requirements with nested data
- Mobile clients with bandwidth constraints
- Third-party integrations with custom data needs

**Implementation**:
- Hot Chocolate or GraphQL.NET
- Schema-first or code-first approach
- DataLoader pattern for N+1 prevention
- Persisted queries for performance

## Event-Driven Messaging

### Azure Service Bus

**Topics and Subscriptions**:
- `domain-events` topic with filtered subscriptions per service
- `notification-requests` queue for notification processing
- `analytics-events` queue for async analytics processing

**Message Structure**:
```json
{
  "eventId": "uuid",
  "eventType": "PostCreated",
  "occurredAt": "ISO 8601 timestamp",
  "tenantId": "tenant-uuid",
  "userId": "user-uuid",
  "payload": {
    "postId": "post-uuid",
    "communityId": "community-uuid",
    "content": "..."
  },
  "metadata": {
    "correlationId": "uuid",
    "causationId": "uuid"
  }
}
```

### Outbox Pattern

Ensures reliable event publishing with transactional guarantees.

**Implementation**:
- `OutboxMessage` table in each service database
- Write domain events to outbox in same transaction as business data
- Background processor reads outbox and publishes to Service Bus
- Mark messages as published after successful delivery
- Retry logic for failed publishes

### Idempotency

Ensures message handlers are safe for duplicate processing.

**Implementation**:
- `ProcessedEvent` table with event ID as key
- Check if event already processed before handling
- Use distributed lock (Redis) for critical sections
- Idempotent message handlers by design where possible

## Security and Authentication

### JWT Token Structure

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "User Name",
  "tenantId": "tenant-id",
  "role": ["Member", "Moderator"],
  "permissions": ["community.read", "post.create"],
  "iat": 1234567890,
  "exp": 1234571490,
  "iss": "https://api.platform.com",
  "aud": "https://api.platform.com"
}
```

**Token Lifecycle**:
- Access tokens: 15-minute expiry
- Refresh tokens: 7-day expiry (sliding)
- Stored in httpOnly cookies or Authorization header
- Revocation via distributed cache blacklist

### OAuth 2.0 Flows

- **Authorization Code Flow**: For web applications
- **PKCE Extension**: For SPAs and mobile apps
- **Client Credentials Flow**: For service-to-service auth
- **Resource Owner Password**: Legacy support only (deprecated)

### Role-Based Access Control (RBAC)

**System Roles**:
- `PlatformAdmin`: Full system access
- `TenantAdmin`: Full access within tenant
- `Moderator`: Content management within communities
- `Member`: Standard user access
- `Guest`: Read-only access (if enabled)

**Permission System**:
- Granular permissions (e.g., `community.create`, `post.delete`, `survey.publish`)
- Role-permission assignments stored in database
- Claims-based authorization in ASP.NET Core
- Policy-based authorization for complex rules

**Implementation**:
```csharp
[Authorize(Policy = "RequireModeratorRole")]
[Authorize(Policy = "RequireSameTenant")]
public async Task<IActionResult> DeletePost(Guid postId) { }
```

### Multi-Tenant Security

**Tenant Isolation**:
- Every request validated for tenant context
- Claims principal includes `tenantId` claim
- Action filters enforce tenant scoping on all queries
- Entity Framework global query filters for tenant discriminator

**Data Access Pattern**:
```csharp
public class TenantScopedDbContext : DbContext
{
    private readonly string _tenantId;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Post>().HasQueryFilter(p => p.TenantId == _tenantId);
    }
}
```

## Data Strategy

### Structured Data (Azure SQL Database)

**Schema Organization**:
- Shared database with tenant discriminator column
- Schema-per-bounded-context (e.g., `users`, `communities`, `surveys`)
- Row-level security policies for additional isolation

**Key Tables**:
- `users.Users`, `users.Roles`, `users.UserRoles`
- `communities.Tenants`, `communities.Communities`, `communities.Memberships`
- `engagement.Posts`, `engagement.Comments`, `engagement.Reactions`
- `surveys.Surveys`, `surveys.Questions`, `surveys.Responses`

**Indexing Strategy**:
- Clustered index on primary key (GUID)
- Non-clustered indexes on tenant ID + frequently queried columns
- Covering indexes for read-heavy queries
- Full-text indexes on searchable content

**Performance Optimization**:
- Connection pooling with appropriate pool size
- Compiled queries for hot paths
- Async/await for all database operations
- Batch operations where applicable

### Content Streams (Azure Cosmos DB)

**Use Cases**:
- Activity feeds (high-volume reads)
- Real-time engagement data
- Event sourcing for audit trails

**Partition Strategy**:
- Partition by `tenantId` for tenant isolation
- Partition by `userId` for user-specific feeds
- Partition by `communityId-date` for temporal queries

**Consistency Level**: Session consistency (default) or eventual for non-critical reads

### Caching Layer (Redis)

**Cache Strategies**:
- **Cache-Aside**: Application manages cache population
- **Read-Through**: Automatic cache population on miss
- **Write-Through**: Update cache and database together

**Cached Data**:
- User sessions and authentication tokens
- Frequently accessed communities and groups
- Tenant configuration and feature flags
- Leaderboards and trending content
- Rate limiting counters

**Key Patterns**:
- `user:{userId}:profile`
- `tenant:{tenantId}:config`
- `community:{communityId}:members`
- `ratelimit:{userId}:{endpoint}:{window}`

**Expiration Strategy**:
- Short TTL (5-15 minutes) for volatile data
- Long TTL (1-24 hours) for stable data
- Cache invalidation via domain events

## Logging, Tracing, and Observability

### Structured Logging (Serilog)

**Configuration**:
- Log to Application Insights
- Log to Azure Blob Storage for long-term retention
- Local console sink for development

**Log Levels**:
- `Verbose`: Detailed trace information
- `Debug`: Development-time debugging
- `Information`: General informational messages
- `Warning`: Potential issues
- `Error`: Exceptions and errors
- `Fatal`: Critical failures

**Structured Properties**:
```csharp
logger.Information(
    "User {UserId} created post {PostId} in community {CommunityId}",
    userId, postId, communityId
);
```

**Sensitive Data**:
- Never log passwords, tokens, or PII
- Mask email addresses and phone numbers
- Use custom destructuring policies for sensitive types

### Distributed Tracing (OpenTelemetry)

**Instrumentation**:
- Automatic instrumentation for ASP.NET Core, EF Core, HTTP clients
- Custom spans for business operations
- Trace context propagation across services and message bus

**Trace Attributes**:
- `tenant.id`, `user.id`, `correlation.id`
- Service name and version
- HTTP method, URL, status code
- Database query type and duration

**Backend**: Application Insights or Jaeger

### Metrics and Monitoring

**Application Metrics**:
- Request rate, duration, and error rate (RED metrics)
- Database query performance
- Cache hit/miss ratio
- Message queue depth and processing time
- Custom business metrics (posts created, surveys completed)

**Infrastructure Metrics**:
- CPU, memory, disk utilization
- Database DTU or vCore usage
- Storage account metrics
- Network throughput

**Dashboards**:
- Real-time operations dashboard (Application Insights)
- Per-tenant usage dashboards
- SLA and uptime monitoring
- Cost monitoring and optimization

**Alerting**:
- Error rate exceeds threshold
- Response time degradation
- Database connection pool exhaustion
- Queue processing backlog
- Dependency failures (external APIs)

## Scaling Patterns

### Horizontal Scaling

**App Service Scale-Out**:
- Auto-scale rules based on CPU, memory, or custom metrics
- Scale out during peak hours (business hours in tenant time zones)
- Minimum 2 instances for HA, scale to 10+ for high load

**Stateless Application Design**:
- No in-memory session state (use Redis)
- No sticky sessions required
- Shared-nothing architecture

### Async Processing

**Background Jobs**:
- Hangfire or Azure Functions for scheduled jobs
- Message-driven processing for long-running operations
- Exponential backoff for retries

**Use Cases**:
- Email sending and notifications
- Report generation
- Data export and import
- Media processing (image resize, video transcoding)
- Analytics aggregation

### CQRS (Command Query Responsibility Segregation)

**Selective Application**:
- Use for high-read scenarios (feeds, analytics)
- Separate read models optimized for queries
- Write models enforce business rules
- Eventual consistency via domain events

**Read Model Updates**:
- Event handlers populate read models
- Projections stored in Redis or Cosmos DB
- Denormalized data for query performance

## Multi-Tenant Strategy

### Shared Database with Discriminator

**Approach**:
- Single database with `TenantId` column on all tenant-scoped tables
- Global query filters in EF Core to enforce tenant scoping
- Row-level security policies in SQL for defense in depth

**Pros**:
- Cost-efficient for large tenant count
- Simplified operations and maintenance
- Easy cross-tenant analytics for platform metrics

**Cons**:
- Noisy neighbor risk (mitigated by resource governance)
- Schema changes affect all tenants
- Requires careful testing of tenant isolation

### Isolation Enforcement

**Application Layer**:
```csharp
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ExtractTenantId(context);
        context.Items["TenantId"] = tenantId;
        // Validate tenant is active
        await _next(context);
    }
}
```

**Database Layer**:
```sql
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_securitypredicate(TenantId)
ON dbo.Posts,
ON dbo.Comments
WITH (STATE = ON);
```

### Tenant-Specific Customizations

**Feature Flags**:
- Per-tenant feature enablement
- A/B testing capabilities
- Gradual rollout control

**Custom Fields**:
- JSON columns for tenant-defined attributes
- Indexed for query performance
- Validated against tenant schema

**Theming**:
- CSS variables for colors, fonts
- Logo and asset uploads to Blob Storage
- CDN distribution for performance

## Technology Choices and Rationale

**Why .NET 8**:
- Cross-platform deployment options
- High performance and throughput
- Rich ecosystem for enterprise features
- Long-term support and stability

**Why Azure SQL Database**:
- Managed service with automatic backups
- Point-in-time restore capability
- Built-in high availability and geo-replication
- Advanced security features (TDE, row-level security)

**Why Azure Service Bus**:
- Reliable message delivery with at-least-once semantics
- Topic/subscription model for event fan-out
- Dead-letter queues for error handling
- Native integration with Azure ecosystem

**Why Redis for Caching**:
- In-memory performance for sub-millisecond latency
- Rich data structures (strings, sets, sorted sets)
- Distributed locking support
- Pub/sub for real-time features

## API Documentation

**OpenAPI/Swagger**:
- Auto-generated from code annotations
- Interactive API explorer in development
- Client SDK generation for frontend

**Developer Portal**:
- API reference documentation
- Authentication guides
- Code samples in multiple languages
- Webhook setup instructions
- Postman collections

## Data Migration and Versioning

**Entity Framework Migrations**:
- Code-first database schema management
- Version-controlled migration scripts
- Applied automatically on deployment (dev/test) or manually (production)

**Zero-Downtime Deployments**:
- Backward-compatible schema changes
- Blue-green deployments for application tier
- Feature flags for new functionality

## Error Handling and Resilience

**Retry Policies**:
- Polly for transient fault handling
- Exponential backoff with jitter
- Circuit breaker for failing dependencies

**Graceful Degradation**:
- Cache stale data when database unavailable
- Queue operations for later processing
- Return partial results with indicators

**Health Checks**:
- ASP.NET Core health check endpoints
- Dependency checks (database, cache, message bus)
- Liveness and readiness probes for orchestration

