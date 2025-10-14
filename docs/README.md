# Documentation Structure

This folder contains all documentation for the Insight Community Platform project.

---

## Quick Navigation

| Document Type | Where to Find It | When to Use It |
|---------------|------------------|----------------|
| **Getting Started** | [`setup/`](setup/) | Setting up your local environment |
| **Architecture Context** | [`contexts/`](contexts/) | Understanding system design |
| **Implementation Guides** | [`implementation/`](implementation/) | Building specific features |
| **Templates** | [`templates/`](templates/) | Creating stories, designs, ADRs |
| **Architecture Decisions** | [`architecture/decisions/`](architecture/decisions/) | Understanding why we made key choices |

---

## Folder Structure

```text
docs/
├── README.md                           # This file
├── context.md                          # High-level system overview
│
├── contexts/                           # Domain-specific architecture
│   ├── analytics-insights.md          # Analytics and reporting
│   ├── backend-architecture.md        # Backend services and APIs
│   ├── domain-model.md                # Core business entities
│   ├── frontend-architecture.md       # Web application
│   ├── infrastructure-devops.md       # Azure infrastructure
│   ├── integrations-extensibility.md  # External integrations
│   ├── mobile-app-architecture.md     # Mobile apps (iOS/Android)
│   ├── security-model.md              # Security and compliance
│   └── tenant-management.md           # Multi-tenancy
│
├── implementation/                     # Phase-by-phase guides
│   ├── phase-0-foundation.md          # Foundation (weeks 1-3)
│   ├── phase-1-mvp.md                 # MVP (weeks 4-11)
│   ├── phase-2-research-tools.md      # Research features
│   └── phase-3-insights.md            # Analysis workspace
│
├── templates/                          # Documentation templates
│   ├── jira-user-story-template.md    # User story format
│   ├── design-doc-template.md         # Feature design docs
│   ├── adr-template.md                # Architecture decisions
│   └── saas-readiness-checklist.md    # SaaS launch/maturity checklist
│
├── setup/                              # Development setup guides
│   ├── development-environment.md     # Local dev setup
│   ├── azure-setup.md                 # Azure provisioning
│   └── troubleshooting.md             # Common issues
│
├── architecture/                       # Architecture decisions
│   └── decisions/                     # ADR records
│       ├── 001-use-clean-architecture.md
│       ├── 002-event-driven-messaging.md
│       └── ...
│
└── guides/                            # How-to guides
    ├── coding-standards.md            # Code conventions
    ├── git-workflow.md                # Branch and PR process
    ├── testing-strategy.md            # Testing guidelines
    └── deployment.md                  # Release process
```

---

## Document Types Explained

### 1. Context Documents (`contexts/`)

**Purpose**: Explain the architecture of specific domains in depth

**When to read**:

- Before working on a feature in that domain
- During architecture reviews
- When onboarding to a specific area

**Examples**:

- Working on surveys? Read `contexts/backend-architecture.md` → Research Engine section
- Building the coding workspace? Read `contexts/analytics-insights.md` → Qualitative Coding section
- Setting up infrastructure? Read `contexts/infrastructure-devops.md`

**These are comprehensive reference documents, not step-by-step tutorials.**

---

### 2. Implementation Guides (`implementation/`)

**Purpose**: Phase-by-phase implementation plans with tasks and acceptance criteria

**When to use**:

- Planning sprints
- Creating Jira stories
- Understanding what to build next
- Tracking progress through phases

**Structure**:

Each phase document contains:

- Overview and timeline
- Milestones with acceptance criteria
- Specific tasks to implement
- Testing requirements
- Definition of done

**Start here when beginning work on a new phase.**

---

### 3. Templates (`templates/`)

**Purpose**: Standardized formats for documentation

**When to use**:

- Creating a new Jira user story → Use `jira-user-story-template.md`
- Designing a complex feature → Use `design-doc-template.md`
- Making an architectural decision → Use `adr-template.md`

**How to use**:

1. Copy the template
2. Fill in the sections
3. Customize as needed for your specific case
4. Save in appropriate location

---

### 4. Architecture Decisions (`architecture/decisions/`)

**Purpose**: Record important architectural choices and rationale

**Format**: Architecture Decision Records (ADRs) numbered sequentially

**When to create**:

- Choosing between technologies (e.g., SQL vs NoSQL)
- Selecting architectural patterns (e.g., monolith vs microservices)
- Making infrastructure decisions (e.g., Azure Service Bus vs RabbitMQ)
- Establishing conventions (e.g., API versioning strategy)

**When to read**:

- Understanding why something is built the way it is
- Before proposing changes to foundational decisions
- During onboarding to learn project philosophy

**Current ADRs**:

- [ADR-001: Use Clean Architecture](architecture/decisions/001-use-clean-architecture.md)

---

### 5. Setup Guides (`setup/`)

**Purpose**: Step-by-step instructions for environment setup

**When to use**:

- First day on the project
- Setting up a new machine
- Troubleshooting environment issues

**Start here**: [Development Environment Setup](setup/development-environment.md)

---

### 6. How-To Guides (`guides/`)

**Purpose**: Procedural guides for common development tasks

**When to use**:

- Need to follow coding standards
- Creating a pull request
- Writing tests
- Deploying to an environment

**Coming soon** (will be added as we establish these):

- Coding standards
- Git workflow
- Testing strategy
- Deployment procedures

---

## Documentation Workflow

### For Developers

**Starting a new feature**:

1. Read relevant context docs → Understand architecture
2. Review phase implementation guide → See task breakdown
3. Copy user story template → Create Jira story
4. For complex features, create design doc
5. Implement feature
6. Update docs if architecture changes

**Making an architectural decision**:

1. Copy ADR template
2. Document context, options, and decision
3. Get team review
4. Number it sequentially (next available number)
5. Save in `architecture/decisions/`

**Adding a new how-to guide**:

1. Create markdown file in `guides/`
2. Follow step-by-step format
3. Include code examples
4. Update this README

---

## Documentation Maintenance

### Who Maintains What

| Document Type | Maintained By | Update Frequency |
|---------------|---------------|------------------|
| Context docs | Tech Lead, Architects | When architecture changes |
| Implementation guides | Tech Lead, PM | Sprint planning |
| ADRs | Anyone (with team approval) | As decisions are made |
| Setup guides | DevOps, Senior Devs | When setup changes |
| Templates | Tech Lead | As needs evolve |

### Review Schedule

- **Context docs**: Reviewed quarterly, updated as needed
- **Implementation guides**: Reviewed after each phase
- **ADRs**: Immutable once accepted (create new ADR if changing)
- **Setup guides**: Tested with each new developer onboarding

### Stale Document Policy

- Mark outdated docs with `[DEPRECATED]` in title
- Add note pointing to replacement
- Archive after 6 months of deprecation

---

## Documentation Standards

### Writing Style

Follow these principles from user rules:

- Use direct, factual language
- Avoid emotional words like "powerful", "amazing"
- No excessive formatting or decorative elements
- No emojis (technical docs are professional)
- Focus on clear structure and practical information
- Write in a sober, professional style

### Markdown Conventions

**Headers**:

```markdown
# H1 - Document title only
## H2 - Major sections
### H3 - Subsections
#### H4 - Details (use sparingly)
```

**Code Blocks**:

Always specify language for syntax highlighting:

````markdown
```csharp
public class Example { }
```

```typescript
const example = () => {};
```

```bash
docker-compose up -d
```
````

**Links**:

- Use relative links for internal docs: `[Setup](setup/development-environment.md)`
- Use absolute URLs for external: `[Azure Docs](https://docs.microsoft.com/azure)`

**Tables**:

Use for structured data:

```markdown
| Column 1 | Column 2 | Column 3 |
|----------|----------|----------|
| Data     | Data     | Data     |
```

---

## Contributing to Docs

### Adding New Documentation

1. **Determine type**: Is it a context, guide, template, or ADR?
2. **Choose location**: Place in appropriate folder
3. **Use template**: If applicable (story, design doc, ADR)
4. **Follow standards**: Match existing style and format
5. **Update this README**: Add to navigation if new category
6. **Create PR**: Include docs in feature PR or separate docs PR

### Updating Existing Documentation

1. **Small fixes**: Grammar, typos, minor clarifications → Direct PR
2. **Major changes**: Architecture changes, new sections → Discuss with team first
3. **Keep history**: Don't delete content, mark as deprecated if superseded
4. **Test instructions**: Ensure setup guides still work

---

## Quick Start for New Team Members

**Day 1**:

1. Read [`context.md`](context.md) - System overview
2. Follow [`setup/development-environment.md`](setup/development-environment.md)
3. Read [ADR-001: Clean Architecture](architecture/decisions/001-use-clean-architecture.md)

**Week 1**:

1. Read domain context docs for your first feature
2. Review implementation guide for current phase
3. Bookmark this README for reference

**Ongoing**:

- Use templates for stories and designs
- Read ADRs to understand decisions
- Contribute to docs as you learn

---

## Documentation Tools

**Viewing Locally**:

- Any markdown viewer
- VS Code with Markdown Preview
- [Grip](https://github.com/joeyespo/grip) for GitHub-style rendering

**Diagrams**:

- ASCII art for simple diagrams (works in markdown)
- [Mermaid](https://mermaid.js.org/) for complex diagrams
- [Excalidraw](https://excalidraw.com/) for architecture diagrams (export as PNG)

**Validation**:

```bash
# Check markdown formatting
npx markdownlint-cli docs/**/*.md

# Check for broken links
npx markdown-link-check docs/**/*.md
```

---

## Questions?

- **Architecture questions**: Tech Lead
- **Process questions**: PM
- **Setup issues**: DevOps team
- **Documentation improvements**: Open a discussion or PR

**Slack Channels**:

- `#documentation` - Docs discussions
- `#architecture` - Architecture questions
- `#development` - General dev help

---

## Document Index

### Core Documentation

- [System Overview](context.md)
- [Development Environment Setup](setup/development-environment.md)
- [Phase 0: Foundation](implementation/phase-0-foundation.md)

### Architecture Contexts

- [Backend Architecture](contexts/backend-architecture.md)
- [Frontend Architecture](contexts/frontend-architecture.md)
- [Domain Model](contexts/domain-model.md)
- [Infrastructure & DevOps](contexts/infrastructure-devops.md)
- [Security Model](contexts/security-model.md)
- [Analytics & Insights](contexts/analytics-insights.md)
- [Mobile App Architecture](contexts/mobile-app-architecture.md)
- [Integrations](contexts/integrations-extensibility.md)
- [Tenant Management](contexts/tenant-management.md)

### Templates

- [Jira User Story Template](templates/jira-user-story-template.md)
- [Design Document Template](templates/design-doc-template.md)
- [ADR Template](templates/adr-template.md)

### Architecture Decisions

- [ADR-001: Clean Architecture](architecture/decisions/001-use-clean-architecture.md)

---

*Last Updated: 2025-01-15*
*Maintainer: Tech Lead*

