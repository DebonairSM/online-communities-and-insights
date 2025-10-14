# ADR-001: Use Clean Architecture with Modular Monolith

**Date**: 2025-01-15
**Status**: Accepted
**Deciders**: Tech Lead, Backend Team
**Technical Story**: ARCH-001

---

## Context

We need to establish the foundational architecture for the Insight Community Platform. The system must:
- Support multiple bounded contexts (Research, Moderation, Analytics, Insights)
- Be maintainable by a small team (3-5 developers initially)
- Allow for future evolution to microservices if needed
- Provide clear separation of concerns
- Enable independent testing of business logic
- Support multiple frontends (web, mobile)

Current situation: Starting from scratch, need to choose architectural pattern that balances simplicity with future flexibility.

---

## Decision Drivers

- **Maintainability**: Team size is small, need clear structure (High priority)
- **Testability**: Business logic must be testable without infrastructure (High)
- **Flexibility**: May need to extract services later (Medium)
- **Team Experience**: Team has .NET experience but varied architectural backgrounds (Medium)
- **Development Speed**: Need to deliver MVP in 8 weeks (High)
- **Complexity Management**: System has 8+ bounded contexts (High)

---

## Considered Options

### Option 1: Clean Architecture (Modular Monolith)

**Description**: Organize code in layers with dependency inversion. Business logic in Core, independent of infrastructure. Start as monolith with clear module boundaries.

**Pros**:
- Clear separation of concerns
- Business logic independent of frameworks
- Highly testable (can test without database)
- Well-documented pattern with .NET examples
- Easy to understand and onboard new developers
- Can extract modules to microservices later if needed
- Single deployment simplifies initial DevOps

**Cons**:
- More boilerplate code than simpler architectures
- Learning curve for developers unfamiliar with pattern
- Slightly more complex for simple CRUD operations
- All modules deployed together (but can optimize later)

**Cost**: No additional infrastructure cost (single deployment)

**Implementation Effort**: Medium (requires proper setup but well-understood)

---

### Option 2: Microservices from Day 1

**Description**: Build as separate services from the start (Research Service, Moderation Service, Analytics Service, etc.)

**Pros**:
- Independent scaling per service
- Clear service boundaries
- Technology flexibility per service
- No future migration needed

**Cons**:
- High operational overhead (multiple deployments)
- Complex local development environment
- Distributed debugging challenges
- Eventual consistency complexity
- Overkill for initial scale (hundreds not thousands of users initially)
- Much slower initial development
- Small team will struggle with operational burden

**Cost**: Higher (multiple App Services or AKS cluster)

**Implementation Effort**: High (distributed systems complexity)

---

### Option 3: Traditional Layered Architecture

**Description**: Simple 3-tier architecture (Presentation, Business Logic, Data Access)

**Pros**:
- Very simple to understand
- Fast initial development
- Minimal boilerplate
- Everyone knows this pattern

**Cons**:
- Business logic often couples to infrastructure
- Hard to test without database
- No clear module boundaries
- Difficult to extract services later
- Tends to become "big ball of mud" over time
- Database-centric design limits flexibility

**Cost**: No additional infrastructure cost

**Implementation Effort**: Low (straightforward)

---

## Decision

**We will use Clean Architecture organized as a Modular Monolith** because it provides the best balance of maintainability, testability, and future flexibility for our context.

**Rationale**:
1. **Clear boundaries**: Each bounded context (Research, Moderation, Analytics) is a distinct module but deploys together
2. **Testable**: Business logic is isolated from infrastructure, enabling fast unit tests
3. **Migration path**: If we need microservices later, module boundaries are clear extraction points
4. **Right-sized**: Matches our team size and current scale while allowing growth
5. **Industry proven**: Many successful .NET projects use this pattern
6. **Team growth**: Clear structure helps onboard new developers

Starting with microservices would be premature optimization that slows us down. Traditional layers would be faster initially but create technical debt. Clean Architecture with modular boundaries gives us the flexibility we need.

---

## Consequences

### Positive
- Business logic can be tested without spinning up database or web server
- Clear separation makes code reviews more focused
- Easy to understand which module owns which functionality
- Can deploy as monolith now, extract services later if needed
- Reduces coupling between modules
- Enables parallel development (teams can work on different modules)

### Negative
- More project structure and files than simple layered approach (mitigate: provide templates and examples)
- Developers need to learn dependency inversion principle (mitigate: training session and code reviews)
- Some additional ceremony for simple CRUD operations (acceptable trade-off)
- All modules must redeploy together initially (mitigate: use feature flags for gradual rollout)

### Neutral
- Need to establish conventions for cross-module communication (use domain events)
- Must decide what goes in Core vs Application vs Infrastructure (document in guidelines)
- Interface proliferation (use pragmatically, not dogmatically)

---

## Implementation Notes

**Project Structure**:
```
src/
├── Core/                         # Domain entities, interfaces
│   ├── Entities/
│   ├── Events/
│   ├── Interfaces/
│   └── ValueObjects/
├── Application/                  # Business logic, use cases
│   ├── Services/
│   │   ├── Research/
│   │   ├── Moderation/
│   │   └── Analytics/
│   ├── Commands/
│   ├── Queries/
│   └── Interfaces/
├── Infrastructure/               # Data access, external services
│   ├── Data/
│   ├── Repositories/
│   ├── Integrations/
│   └── Messaging/
└── Api/                         # Web API, controllers

tests/
├── Core.Tests/
├── Application.Tests/
└── Integration.Tests/
```

**Key Principles**:
1. **Core** has no dependencies on other projects
2. **Application** depends only on Core
3. **Infrastructure** implements interfaces from Core/Application
4. **Api** orchestrates and depends on Application
5. Use dependency injection to wire up implementations
6. Domain events for cross-module communication

**Timeline**: 
- Week 1: Set up project structure and base classes
- Week 2: Implement first module (Authentication) as reference
- Week 3: Document patterns and guidelines for team

**Migration from Layers (if needed)**: N/A (greenfield project)

---

## Validation

**Success Criteria**:
- [✓] Core project has zero dependencies on external packages (except maybe FluentValidation)
- [ ] Unit tests for business logic run in <5 seconds (fast feedback)
- [ ] New developer can understand structure within 1 day
- [ ] Can add new feature without touching multiple layers
- [ ] Code reviews focus on business logic not infrastructure plumbing

**Review Date**: 2025-07-15 (6 months after implementation)

**Metrics to Track**:
- Unit test execution time
- Build time
- Time to add new feature (by module)
- Developer satisfaction with structure

---

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [Modular Monolith](https://www.kamilgrzybek.com/blog/posts/modular-monolith-primer)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- Related: Will create ADR-002 (Event-Driven Communication Between Modules)

---

## Decision Log

| Date | Author | Change |
|------|--------|--------|
| 2025-01-15 | Tech Lead | Initial proposal drafted |
| 2025-01-16 | Backend Team | Reviewed and accepted |
| 2025-01-20 | Tech Lead | Implemented project structure |

---

## Appendix: Layer Responsibilities

### Core Layer
**What**: Domain entities, business rules, domain events, core interfaces
**Examples**: User, Tenant, Survey, Post, Theme entities
**Dependencies**: None (pure domain logic)

### Application Layer
**What**: Use cases, business logic orchestration, application services
**Examples**: AuthService, SurveyService, CodingService
**Dependencies**: Core only

### Infrastructure Layer
**What**: Database access, external API clients, message bus, file storage
**Examples**: EF Core repositories, Azure Blob client, Service Bus publisher
**Dependencies**: Core, Application (implements interfaces)

### API Layer
**What**: HTTP endpoints, request/response models, authentication, authorization
**Examples**: AuthController, SurveysController, middleware
**Dependencies**: Application (consumes services)

