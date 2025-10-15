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
- **[Social Login Setup](implementation/social-login-setup.md)** - OAuth configuration guide

## 🏗️ Architecture

### Core Architecture
- **[Backend Architecture](contexts/backend-architecture.md)** - .NET services, APIs, and data strategy
- **[Frontend Architecture](contexts/frontend-architecture.md)** - React SPA, components, and state management  
- **[Mobile App Architecture](contexts/mobile-app-architecture.md)** - React Native app for participants
- **[Infrastructure & DevOps](contexts/infrastructure-devops.md)** - Azure deployment and operations

### Domain Design
- **[Domain Model](contexts/domain-model.md)** - Business entities and relationships
- **[Security Model](contexts/security-model.md)** - Authentication, authorization, and compliance
- **[Analytics & Insights](contexts/analytics-insights.md)** - Data pipeline and reporting

### Technology Stack
- **[Tech Stack](contexts/TechStack.context.md)** - Complete technology choices and packages
- **[Integrations & Extensibility](contexts/integrations-extensibility.md)** - External system integration  
- **[Tenant Management](contexts/tenant-management.md)** - Multi-tenant SaaS considerations

## 📝 Architecture Decisions (ADRs)

Important architectural decisions are tracked in `architecture/decisions/`:

1. **[Clean Architecture](architecture/decisions/001-use-clean-architecture.md)** - Modular monolith approach
2. **[Azure Media Services](architecture/decisions/002-azure-media-services.md)** - Video processing strategy
3. **[Azure Communication Services](architecture/decisions/003-azure-communication-services.md)** - Email notifications
4. **[Azure Cognitive Services](architecture/decisions/004-azure-cognitive-services.md)** - AI transcription
5. **[Multi-Tenant Database](architecture/decisions/005-shared-db-composite-keys.md)** - Data isolation strategy
6. **[Authentication Strategy](architecture/decisions/006-authentication-strategy.md)** - Multi-phase auth approach
7. **[Self-Issued JWT Tokens](architecture/decisions/007-self-issued-jwt-tokens.md)** - Token format decision

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
| Learn the architecture | [Backend](contexts/backend-architecture.md) + [Frontend](contexts/frontend-architecture.md) |
| Understand the domain | [Domain Model](contexts/domain-model.md) |
| Configure authentication | [Social Login Setup](implementation/social-login-setup.md) |
| Deploy to Azure | [Infrastructure & DevOps](contexts/infrastructure-devops.md) |
| Add a new feature | Check [ADRs](architecture/decisions/) + [Domain Model](contexts/domain-model.md) |

## 📁 Directory Structure

```
docs/
├── README.md                    # This navigation guide
├── GETTING-STARTED.md          # Development setup
├── OVERVIEW.md                 # System overview (renamed from context.md)
│
├── architecture/               # Architectural decisions
│   └── decisions/             # ADRs (Architecture Decision Records)
│
├── contexts/                  # Detailed architecture docs
│   ├── backend-architecture.md
│   ├── frontend-architecture.md
│   └── ...
│
├── implementation/            # Implementation guides and status
│   ├── status.md             # Current progress
│   ├── roadmap.md            # Delivery phases
│   └── ...
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