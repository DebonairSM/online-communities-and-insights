# Online Communities Platform Documentation

Welcome to the documentation for the **Insight Community Platform** - a comprehensive SaaS solution for building and managing research-focused online communities.

## 🚀 Quick Start

- **[Getting Started Guide](GETTING-STARTED.md)** - Set up your development environment  
- **[System Overview](OVERVIEW.md)** - Product vision, personas, and high-level architecture
- **[Implementation Status](implementation/status.md)** - Current progress and next steps

## 📋 Implementation

### Current Progress
- **[Implementation Status](implementation/status.md)** - What's built and what's next
- **[Implementation Roadmap](implementation/roadmap.md)** - Phased delivery plan

### Setup Guides  
- **[Development Environment](setup/development-environment.md)** - Local development setup
- **[Phase 0: Foundation](implementation/phase-0-foundation.md)** - Core architecture setup
- **[Social Login Setup](backend/implementation/social-login-setup.md)** - OAuth configuration guide

## 🏗️ Architecture

### Backend
- **[Backend Architecture](backend/backend-architecture.md)** - .NET services, APIs, and data strategy
- **[Domain Model](backend/domain-model.md)** - Business entities and relationships
- **[Security Model](backend/security-model.md)** - Authentication, authorization, and compliance
- **[Infrastructure & DevOps](backend/infrastructure-devops.md)** - Azure deployment and operations
- **[Analytics & Insights](backend/analytics-insights.md)** - Data pipeline and reporting
- **[Tech Stack](backend/TechStack.context.md)** - Complete technology choices and packages
- **[Integrations & Extensibility](backend/integrations-extensibility.md)** - External system integration  
- **[Tenant Management](backend/tenant-management.md)** - Multi-tenant SaaS considerations

### Frontend
- **[Frontend Architecture](frontend/frontend-architecture.md)** - React SPA, components, and state management  
- **[Mobile App Architecture](frontend/mobile-app-architecture.md)** - React Native app for participants

## 📝 Architecture Decisions (ADRs)

Important architectural decisions are tracked in `backend/architecture-decisions/`:

1. **[Clean Architecture](backend/architecture-decisions/001-use-clean-architecture.md)** - Modular monolith approach
2. **[Azure Media Services](backend/architecture-decisions/002-azure-media-services.md)** - Video processing strategy
3. **[Azure Communication Services](backend/architecture-decisions/003-azure-communication-services.md)** - Email notifications
4. **[Azure Cognitive Services](backend/architecture-decisions/004-azure-cognitive-services.md)** - AI transcription
5. **[Multi-Tenant Database](backend/architecture-decisions/005-shared-db-composite-keys.md)** - Data isolation strategy
6. **[Authentication Strategy](backend/architecture-decisions/006-authentication-strategy.md)** - Multi-phase auth approach
7. **[Self-Issued JWT Tokens](backend/architecture-decisions/007-self-issued-jwt-tokens.md)** - Token format decision

## 📚 Resources

### Templates
- **[ADR Template](templates/adr-template.md)** - Architecture decision format
- **[Design Document Template](templates/design-doc-template.md)** - Feature design format
- **[User Story Template](templates/jira-user-story-template.md)** - Jira story format
- **[SaaS Readiness Checklist](templates/saas-readiness-checklist.md)** - Production readiness

### Project Planning
- **[Project Kickstart](contexts/project-kickstart.md)** - Implementation roadmap and team structure

## 🧭 Navigation Guide

| **I want to...** | **Start here** |
|---|---|
| Set up development environment | [Getting Started](GETTING-STARTED.md) |
| Understand what we're building | [System Overview](OVERVIEW.md) |
| See what's implemented | [Implementation Status](implementation/status.md) |
| Learn the architecture | [Backend](backend/backend-architecture.md) + [Frontend](frontend/frontend-architecture.md) |
| Understand the domain | [Domain Model](backend/domain-model.md) |
| Configure authentication | [Social Login Setup](backend/implementation/social-login-setup.md) |
| Deploy to Azure | [Infrastructure & DevOps](backend/infrastructure-devops.md) |
| Add a new feature | Check [ADRs](backend/architecture-decisions/) + [Domain Model](backend/domain-model.md) |

## 📁 Directory Structure

```
docs/
├── README.md                    # This navigation guide
├── GETTING-STARTED.md          # Development setup
├── OVERVIEW.md                 # System overview
│
├── frontend/                   # Frontend-specific documentation
│   ├── frontend-architecture.md
│   ├── mobile-app-architecture.md
│   └── implementation/         # Frontend implementation guides
│
├── backend/                    # Backend-specific documentation
│   ├── backend-architecture.md
│   ├── domain-model.md
│   ├── security-model.md
│   ├── infrastructure-devops.md
│   ├── analytics-insights.md
│   ├── integrations-extensibility.md
│   ├── tenant-management.md
│   ├── TechStack.context.md
│   ├── architecture-decisions/ # ADRs (Architecture Decision Records)
│   └── implementation/         # Backend implementation guides
│
├── implementation/            # Cross-cutting implementation guides
│   ├── status.md             # Current progress
│   ├── roadmap.md            # Delivery phases
│   └── phase-0-foundation.md
│
├── contexts/                  # Remaining general context docs
│   └── project-kickstart.md
│
├── setup/                    # Development setup guides
└── templates/                # Documentation templates
```

## ✅ Contributing

When adding documentation:
1. Use appropriate templates from `templates/`  
2. Link to related documents
3. Update this README if adding new sections
4. Follow the established directory structure
5. Create ADRs for significant architectural decisions

### Writing Standards

- Use direct, factual language without marketing terms
- No emojis in technical documentation  
- Focus on clear structure and practical information
- Include code examples with syntax highlighting
- Use relative links for internal docs

---

*This documentation evolves with the platform. Keep it current as the system grows.*