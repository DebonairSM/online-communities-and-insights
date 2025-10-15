# System Overview

## Product Vision

This platform is an **Insight Community Platform** modeled after C Space, designed for always-on, private research communities where brands conduct continuous qualitative and quantitative research. Unlike general social networks, this is a dedicated research platform where every feature serves insight generation. Organizations build trusted, curated spaces for participants to share experiences, opinions, and behaviors through mixed-method research activities including surveys, video diaries, photo annotations, in-depth interviews, and collaborative exercises. The platform combines real-time engagement with rigorous research methodologies, human moderation, and narrative-driven reporting to deliver actionable insights.

## Strategic Goals

- Enable brands to conduct always-on market research in private, curated insight communities
- Seamlessly blend qualitative research methods (diaries, interviews, ethnography) with quantitative surveys and polls
- Support digital ethnography with rich media capture (photo/video diaries, annotations, collaging)
- Provide web and native mobile apps for in-situ research participation
- Empower human moderators to guide discussions, maintain quality, and create safe spaces
- Transform raw research data into narrative-driven "insight stories" for stakeholder presentations
- Maintain research-grade security, consent management, and participant privacy
- Support integration with research tools (Qualtrics, SPSS, MaxQDA) and enterprise systems
- Scale to support hundreds of brand communities with thousands of participants each

## User Personas

### Research Participant (Community Member)
End users recruited for research studies who complete tasks, respond to surveys, share media diaries, participate in discussions, and provide authentic insights. Authentication via social login (Google, GitHub, Microsoft personal accounts) or email/password (future). Compensated through incentive systems (points, gift cards). Privacy-conscious with explicit consent for each study.

### Moderator / Facilitator
Trained research professionals who guide discussions, prompt deeper responses, maintain community guidelines, and ensure quality contributions. Responsible for creating a safe, trusted environment. Can approve/reject content, manage participants, and tag responses with research themes. Often have qualitative research backgrounds.

### Client Analyst / Research Manager
Brand-side researchers and insights professionals who design research activities, analyze responses, code qualitative data, and compile findings. They use the Insight Workspace to tag themes, select quotes, and build narrative insight stories for stakeholder presentations.

### Brand Administrator
Senior client stakeholders who oversee the research community, configure branding, manage budgets, invite research teams, and access high-level analytics. Control tenant settings, incentive programs, and data retention policies.

### Platform Administrator
Internal staff who provision brand communities, manage infrastructure, ensure compliance, and provide technical support. Access to cross-tenant operations for platform health monitoring and troubleshooting.

## Functional Domains

### Research Engine (Core Domain)
- **Quantitative Tools**: Survey builder with logic/branching, polls, rating scales, MaxDiff, conjoint
- **Qualitative Tools**: Video/photo diaries, image annotation, collaging, discussion prompts
- **In-Depth Interviews (IDIs)**: Scheduling, consent capture, video/audio recording, transcription
- **Focus Groups**: Virtual session hosting, participant management, recording, chat transcripts
- **Task Management**: Activity briefs, deadlines, reminders, completion tracking
- **Sampling & Quotas**: Automated participant selection based on demographics and behavior
- **Mobile Capture**: Native app support for in-situ photo/video capture with offline sync

### Moderation & Quality Control
- **Moderation Dashboard**: Review queue for pending contributions, approval workflows
- **Content Guidelines**: Community rules, automated flagging, manual review
- **Participant Management**: Engagement tracking, quality scoring, incentive calculation
- **Safety Controls**: Profanity filtering, PII detection, member blocking, escalation workflows
- **Facilitation Tools**: Prompt participants, ask follow-up questions, guide discussions

### Insight Workspace (Analysis Domain)
- **Qualitative Coding**: Tag responses with themes, create coding taxonomy, inter-coder reliability
- **Mixed-Method Integration**: Merge survey statistics with coded qualitative responses
- **Sentiment & Theme Clustering**: AI-assisted grouping of similar responses
- **Insight Story Builder**: Narrative report creation with quotes, media clips, charts
- **Quote Management**: Select impactful quotes, attribute or anonymize, organize by theme
- **Visualization**: Charts, word clouds, journey maps, persona cards
- **Export Options**: PowerPoint, PDF, video reels, Excel, SPSS, MaxQDA, NVivo

### Participant Experience
- **Activity Feed**: Personalized research tasks, discussions, polls
- **Rich Media Sharing**: Photo/video upload, annotation tools, emoji reactions
- **Profile & Preferences**: Demographics, interests, notification settings
- **Incentives**: Points balance, reward catalog, redemption tracking
- **Privacy Controls**: Consent management, data access requests, anonymity options
- **Mobile App**: Native iOS/Android with camera, offline mode, push notifications

### Admin & Operations
- **Community Setup**: Branding, participant recruitment, moderator assignment
- **Research Design**: Task templates library, activity scheduling, quota configuration
- **Budget Management**: Incentive pools, participant compensation, usage tracking
- **Compliance**: Consent logging, data retention, export controls, audit trails
- **Integrations**: CRM sync, research tool exports, BI dashboards

## High-Level Architecture

### Frontend Layer
**Web Application**: Modern SPA (React 18+ with TypeScript) with research-focused UI components. Supports moderation dashboards, insight workspaces, qualitative coding tools, and admin panels.

**Mobile Applications**: Native iOS and Android apps (React Native or Flutter) for research participants. Critical for in-situ ethnographic capture (photo/video diaries), offline task completion with background sync, push notifications for research activities, and camera/microphone access.

**Common Features**:
- Per-tenant branding and white-labeling
- Real-time updates via SignalR
- Rich media upload and annotation
- Accessibility and localization support

### Backend Layer
Modular monolith built on .NET 9+ with clear service boundaries:
- **Authentication**: OAuth 2.0 social login with JWT tokens, flexible multi-provider support
- **Research Engine**: Survey/poll/diary/interview management with quota enforcement
- **Media Processing**: Video transcoding, image annotation, auto-transcription (Azure AI Services)
- **Moderation Service**: Content review queues, quality scoring, safety filters
- **Insight Service**: Qualitative coding, theme tagging, story compilation
- **Incentive Service**: Points tracking, reward fulfillment, budget management
- **Consent Service**: Study-level consent capture, GDPR compliance workflows
- Event-driven communication via Azure Service Bus
- Async processing for transcription, video encoding, export generation

### Infrastructure Layer
Azure-hosted with research-specific considerations:
- App Services (Premium tier) for web APIs and background jobs
- Azure SQL Database for structured research data (surveys, responses, themes)
- Azure Cosmos DB for activity streams and real-time feeds
- Azure Blob Storage for participant media (video diaries, photos) with lifecycle management
- Azure Media Services for video transcoding and streaming
- Azure Cognitive Services for transcription, translation, sentiment analysis
- Redis Cache for session management and response caching
- Application Insights for observability and research activity tracking

## Core Technology Stack

### Backend
- **Runtime**: .NET 9
- **API Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9
- **Authentication**: OAuth 2.0 (Google, GitHub, Microsoft) + JWT tokens
- **Messaging**: Azure Service Bus
- **Caching**: StackExchange.Redis
- **Testing**: xUnit, FluentAssertions, Moq

### Web Frontend
- **Framework**: React 18+ with TypeScript
- **State Management**: Redux Toolkit with RTK Query
- **UI Components**: Material-UI with custom research components
- **API Client**: Axios with OpenAPI-generated types
- **Real-time**: SignalR client for live updates
- **Build**: Vite
- **Rich Text**: TipTap or ProseMirror for qualitative responses
- **Media**: React-Player for video playback, Fabric.js for image annotation
- **Authentication**: Standard OAuth 2.0 flows (no MSAL for social login)

### Mobile Frontend
- **Framework**: React Native (cross-platform iOS/Android) or Flutter
- **State Management**: Redux Toolkit (shared with web where possible)
- **Media Capture**: Expo Camera API or native camera modules
- **Offline Storage**: SQLite + Redux Persist for offline task completion
- **Background Sync**: WorkManager (Android) / Background Tasks (iOS)
- **Push Notifications**: Firebase Cloud Messaging or APNs
- **Media Upload**: Chunked upload with retry logic for large videos

### Infrastructure
- **Cloud Platform**: Microsoft Azure
- **Compute**: Azure App Services (with scale-out) or AKS
- **Database**: Azure SQL Database (Business Critical tier for production)
- **NoSQL**: Azure Cosmos DB (for activity streams)
- **Cache**: Azure Cache for Redis (Premium tier)
- **Storage**: Azure Blob Storage (hot tier for active media)
- **CDN**: Azure Front Door or CDN
- **Secrets**: Azure Key Vault
- **Monitoring**: Application Insights, Log Analytics

### DevOps
- **Source Control**: Git (GitHub or Azure Repos)
- **CI/CD**: GitHub Actions or Azure DevOps Pipelines
- **IaC**: Bicep (primary) or Terraform
- **Containers**: Docker (if using AKS)
- **API Gateway**: Azure API Management (optional)

### Observability
- **Logging**: Serilog with structured logging
- **Tracing**: OpenTelemetry + Application Insights
- **Metrics**: Application Insights metrics + custom counters
- **Dashboards**: Azure Workbooks or Grafana

## Integration Strategy

### Research Tool Integration
- **Qualtrics**: Export survey designs, import panel data, sync responses
- **SPSS / SAS**: Statistical analysis export in native formats
- **MaxQDA / NVivo / ATLAS.ti**: Qualitative data export with coding taxonomy
- **Survey Platforms**: Import questions, export responses to Confirmit, Decipher
- **Transcription Services**: Auto-transcribe via Azure Cognitive Services, Rev, Otter.ai

### CRM Integration
- Bidirectional sync of participant data with Salesforce, Dynamics 365, HubSpot
- Segment participants based on CRM attributes
- Track research participation in CRM activity logs
- Custom field mapping per brand community

### BI Tools Integration
- **Power BI / Tableau**: Direct query access to insight data marts, pre-built dashboards
- **Excel**: Response exports with pivot tables, cross-tabs
- **Data Warehouse**: Scheduled exports to Snowflake, Databricks for advanced analytics
- REST API for custom research repositories

### Media & AI Services
- **Azure Media Services**: Video transcoding, streaming, thumbnail generation
- **Azure Cognitive Services**: Auto-transcription, translation (50+ languages), sentiment analysis
- **Custom AI Models**: Theme clustering, emotion detection from video
- **Secure Video Storage**: SFTP/REST ingestion for large ethnographic video datasets

### Authentication Integration
- **Phase 2 (Current)**: OAuth 2.0 social login (Google, GitHub, Microsoft personal accounts)
- **Phase 1 (Future)**: Email/password with optional Microsoft Entra External ID for managed auth
- **Phase 3 (Future)**: Enterprise SSO via Microsoft Entra ID (multi-tenant), Okta, Auth0, SAML 2.0
- Per-brand authentication configuration

## Compliance & Security Principles

### SOC 2 Type II
- Continuous monitoring and logging
- Quarterly security assessments
- Incident response procedures
- Change management controls
- Vendor risk management

### GDPR Compliance
- **Article 9 Sensitive Data**: Special protections for health, biometric, genetic data in research
- **Explicit Consent**: Study-level consent capture with clear purpose explanations
- **Right to Access**: Participants can export all their research contributions
- **Right to Erasure**: Anonymization workflows that preserve research integrity
- **Right to Withdraw**: Participants can exit studies with data retention options
- **Data Minimization**: Collect only research-necessary PII
- **Purpose Limitation**: Separate consent for each research study or use case
- **EU Data Residency**: Option for EU-only participant data storage

### Research Ethics & Consent
- **Informed Consent**: Per-study consent forms with IRB-style disclosures
- **Consent Logging**: Audit trail of when, where, and what participants consented to
- **Minor Protection**: Age verification, parental consent workflows for under-18 research
- **Vulnerable Populations**: Extra safeguards for sensitive participant groups
- **Data Use Transparency**: Clear explanation of how insights will be used by brands
- **Withdrawal Rights**: Easy opt-out with data retention/deletion choices

### HIPAA-Lite (Health Research)
- **PHI Protection**: When researching health topics, additional safeguards for identifiable health data
- **De-identification**: Automatic PII masking for health-related research exports
- **Access Controls**: Strict role-based access to health-sensitive research data
- **Audit Logging**: Comprehensive tracking of PHI access

### Tenant Isolation
- Logical isolation via tenant discriminator in shared tables
- Row-level security policies in database
- Per-tenant encryption keys (optional)
- Resource quotas and rate limits per tenant
- Separate Azure resources for high-security tenants (on request)

### Security Controls
- Encryption at rest (TDE for SQL, server-side encryption for storage)
- Encryption in transit (TLS 1.2+ required)
- Token-based authentication (JWT with short expiry)
- Role-based access control (RBAC) with least privilege
- Input validation and output encoding
- SQL injection prevention via parameterized queries
- XSS protection via Content Security Policy
- DDoS protection via Azure Front Door
- Regular security scanning (SAST/DAST)
- Penetration testing (annual)

### Media Privacy & Anonymization
- **Separate Storage**: Identifiable media (faces, voices) stored separately from research data
- **Access Control**: Media accessible only to authorized researchers with explicit consent
- **Automatic Blurring**: Optional face blurring in photos/videos for anonymized exports
- **Voice Distortion**: Audio masking option for sensitive recordings
- **Watermarking**: Brand-specific watermarks on exported media to prevent misuse
- **Redaction Tools**: Manual PII removal from images (license plates, addresses, documents)
- **Retention Policies**: Configurable media deletion after research completion

### Audit & Compliance
- Comprehensive audit logging (who, what, when, where)
- Retention policies aligned with regulatory requirements
- Immutable audit logs in append-only storage
- Compliance dashboard for client admins
- Automated compliance reports

## Extension Points

The architecture includes the following extension mechanisms:

- **Custom Fields**: Tenant-defined fields on core entities (User, Community, Post)
- **Webhooks**: Subscribe to domain events for custom workflows
- **Plugin System**: (Future) Load tenant-specific modules for custom features
- **Theming**: CSS variables and asset uploads for white-labeling
- **API Extensions**: Tenant-specific API endpoints for unique requirements
- **Integration Adapters**: Custom adapters for proprietary systems

## Next Steps

Refer to domain-specific context documents in `docs/contexts/` for detailed architectural specifications:
- `backend-architecture.md` - Research Engine, moderation, insight workspace, API design
- `frontend-architecture.md` - Web UI components, moderation dashboards, coding tools
- `mobile-app-architecture.md` - Native apps for iOS/Android, offline sync, media capture
- `infrastructure-devops.md` - Azure deployment, CI/CD, monitoring, disaster recovery
- `analytics-insights.md` - Mixed-method reporting, insight stories, qual+quant integration
- `domain-model.md` - Research entities, consent, incentives, themes, focus groups
- `security-model.md` - Research ethics, consent management, media anonymization
- `integrations-extensibility.md` - Research tool exports, transcription, qual software integration
- `tenant-management.md` - Brand community lifecycle, campaign billing, moderator assignment
- `project-kickstart.md` - C Space-style platform implementation roadmap
