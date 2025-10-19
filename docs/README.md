# Online Communities Platform Documentation

Welcome to the documentation for the **Insight Community Platform** - a comprehensive SaaS solution for building and managing research-focused online communities.

## 🚀 Quick Start

- **[Getting Started Guide](GETTING-STARTED.md)** - Set up your development environment  
- **[System Overview](OVERVIEW.md)** - Product vision, personas, and high-level architecture
- **[Implementation Status](implementation/status.md)** - Current progress and next steps

## 📋 Implementation

### Current Progress
- **[Implementation Status](implementation/status.md)** - What's built and what's next (Phase 0 Complete ✅)
- **[Phase 0 Complete](implementation/PHASE-0-COMPLETE.md)** - Foundation completion summary
- **[Implementation Roadmap](implementation/roadmap.md)** - Phased delivery plan

### Setup Guides  
- **[Azure Configuration Steps](azure/AZURE-CONFIGURATION-STEPS.md)** - Quick start for Azure setup (START HERE)
- **[Azure Setup Guide](setup/AZURE-SETUP-GUIDE.md)** - Detailed Microsoft Entra External ID configuration
- **[Development Environment](setup/development-environment.md)** - Local development setup
- **[Phase 0: Foundation](implementation/phase-0-foundation.md)** - Core architecture setup

## 🏗️ Architecture

### Backend
- **[Backend Architecture](backend/README.md)** - .NET services, APIs, and data strategy
- **[Domain Model](backend/domain-model.md)** - Business entities and relationships
- **[Authentication](backend/authentication.md)** - Security, authorization, and compliance
- **[Infrastructure](backend/infrastructure.md)** - Azure deployment and operations
- **[Analytics](backend/analytics.md)** - Data pipeline and reporting
- **[Tech Stack](backend/tech-stack.md)** - Technology choices and packages
- **[Integrations](backend/integrations.md)** - External system integration  
- **[Multi-Tenancy](backend/multi-tenancy.md)** - Multi-tenant architecture
- **[Architecture Decisions](backend/architecture-decisions.md)** - Key architectural choices

### Frontend
- **[Frontend Architecture](frontend/frontend-architecture.md)** - React SPA, components, and state management  
- **[Mobile App Architecture](frontend/mobile-app-architecture.md)** - React Native app for participants

## 📝 Architecture Decisions (ADRs)

Important architectural decisions are documented in **[Architecture Decisions](backend/architecture-decisions.md)**:

1. **Clean Architecture** - Modular monolith approach with clear layer separation
2. **Azure Media Services** - Video processing strategy
3. **Azure Communication Services** - Email and SMS notifications
4. **Azure Cognitive Services** - AI transcription and sentiment analysis
5. **Multi-Tenant Database** - Shared database with composite keys for isolation
6. **Authentication Strategy** - Microsoft Entra External ID for managed authentication
7. **Token Validation** - Microsoft-issued JWT tokens with custom claims
8. **OAuth Migration** - Removed self-issued JWTs in favor of Entra External ID exclusively

## 📚 Resources

### Azure Configuration
- **[Azure Configuration Steps](azure/AZURE-CONFIGURATION-STEPS.md)** - Quick start for Azure setup
- **[Entra Roles Setup](azure/AZURE-ENTRA-ROLES-SETUP.md)** - Custom attributes and roles configuration
- **[Token Enrichment Quick Reference](azure/ENTRA-TOKEN-ENRICHMENT-QUICK-REFERENCE.md)** - Hard-to-find token enrichment details

### Feature Guides
- **[Chat Feature Guide](features/CHAT-FEATURE-GUIDE.md)** - Real-time chat implementation
- **[Demo Script](features/DEMO-SCRIPT.md)** - Platform demonstration walkthrough

### Implementation Guides
- **[CQRS Implementation Summary](guides/CQRS-IMPLEMENTATION-SUMMARY.md)** - Command Query Responsibility Segregation
- **[Implementation Summary](guides/IMPLEMENTATION-SUMMARY.md)** - Overall implementation overview
- **[Documentation Cleanup Summary](guides/DOCUMENTATION-CLEANUP-SUMMARY.md)** - Documentation organization

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
| Learn the architecture | [Backend](backend/README.md) + [Frontend](frontend/frontend-architecture.md) |
| Understand the domain | [Domain Model](backend/domain-model.md) |
| Configure authentication | [Azure Configuration Steps](azure/AZURE-CONFIGURATION-STEPS.md) |
| Deploy to Azure | [Infrastructure](backend/infrastructure.md) |
| Add a new feature | Check [ADRs](backend/architecture-decisions/) + [Domain Model](backend/domain-model.md) |

## 📁 Directory Structure

```
docs/
├── README.md                    # This navigation guide
├── GETTING-STARTED.md          # Development setup
├── OVERVIEW.md                 # System overview
├── QUICK-START.md              # Quick start guide
│
├── azure/                      # Azure configuration guides
│   ├── AZURE-CONFIGURATION-STEPS.md
│   ├── AZURE-ENTRA-ROLES-SETUP.md
│   └── ENTRA-TOKEN-ENRICHMENT-QUICK-REFERENCE.md
│
├── features/                   # Feature-specific guides
│   ├── CHAT-FEATURE-GUIDE.md
│   └── DEMO-SCRIPT.md
│
├── guides/                     # Implementation guides
│   ├── CQRS-IMPLEMENTATION-SUMMARY.md
│   ├── IMPLEMENTATION-SUMMARY.md
│   └── DOCUMENTATION-CLEANUP-SUMMARY.md
│
├── frontend/                   # Frontend-specific documentation
│   ├── frontend-architecture.md
│   ├── mobile-app-architecture.md
│   └── implementation/         # Frontend implementation guides
│
├── backend/                    # Backend-specific documentation
│   ├── README.md
│   ├── architecture-decisions.md
│   ├── authentication.md
│   ├── domain-model.md
│   ├── infrastructure.md
│   ├── analytics.md
│   ├── integrations.md
│   ├── multi-tenancy.md
│   └── tech-stack.md
│
├── implementation/            # Cross-cutting implementation guides
│   ├── status.md             # Current progress
│   ├── roadmap.md            # Delivery phases
│   └── phase-0-foundation.md
│
├── contexts/                  # General context docs
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