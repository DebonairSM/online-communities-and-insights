# ADR-XXX: [Decision Title]

**Date**: YYYY-MM-DD
**Status**: [Proposed | Accepted | Deprecated | Superseded by ADR-YYY]
**Deciders**: [Names of people involved in decision]
**Technical Story**: [Jira Epic/Story if applicable]

---

## Context

What is the issue we're facing that motivates this decision?

Provide background information including:
- Current situation
- Problem or opportunity
- Constraints and requirements
- Stakeholders affected

**Example**:
```
We need to choose an event-driven messaging system for asynchronous 
communication between services. The system must support:
- Reliable message delivery
- Topic/subscription patterns for event fan-out
- Dead-letter queues for error handling
- Integration with Azure ecosystem
- Scale to 10,000+ messages/second

Current situation: Services communicate synchronously via HTTP, causing
tight coupling and making it difficult to add event-driven workflows.
```

---

## Decision Drivers

Key factors influencing the decision:

- **Factor 1**: [Description and priority]
- **Factor 2**: [Description and priority]
- **Factor 3**: [Description and priority]

**Example**:
```
- Must integrate seamlessly with Azure (High priority)
- Must support at-least-once delivery guarantees (High)
- Cost-effective for our scale (Medium)
- Team familiarity preferred but not required (Low)
- Must support message ordering within topic (Medium)
```

---

## Considered Options

### Option 1: [Name]

**Description**: Brief overview of this option

**Pros**:
- Pro 1
- Pro 2
- Pro 3

**Cons**:
- Con 1
- Con 2
- Con 3

**Cost**: [Estimated cost if applicable]

**Implementation Effort**: [Low | Medium | High]

---

### Option 2: [Name]

**Description**: Brief overview of this option

**Pros**:
- Pro 1
- Pro 2

**Cons**:
- Con 1
- Con 2

**Cost**: [Estimated cost if applicable]

**Implementation Effort**: [Low | Medium | High]

---

### Option 3: [Name]

**Description**: Brief overview of this option

**Pros**:
- Pro 1
- Pro 2

**Cons**:
- Con 1
- Con 2

**Cost**: [Estimated cost if applicable]

**Implementation Effort**: [Low | Medium | High]

---

## Decision

**We will** [chosen option] **because** [rationale].

**Example**:
```
We will use Azure Service Bus because it provides the best integration
with our Azure ecosystem, offers reliable message delivery with built-in
retry and dead-letter queues, and supports the topic/subscription model
we need for event fan-out. While it comes with vendor lock-in, the
operational benefits and reduced maintenance burden outweigh this concern
for our use case.
```

---

## Consequences

### Positive
- [Positive consequence 1]
- [Positive consequence 2]
- [Positive consequence 3]

### Negative
- [Negative consequence 1 and how we'll mitigate]
- [Negative consequence 2 and how we'll mitigate]

### Neutral
- [Neutral consequence 1]
- [Neutral consequence 2]

**Example**:
```
Positive:
- Native Azure integration reduces operational overhead
- Built-in monitoring via Application Insights
- Automatic retries and dead-letter queues
- Team can use existing Azure knowledge

Negative:
- Azure vendor lock-in (mitigate: abstract behind interface)
- Higher cost than self-hosted RabbitMQ (acceptable for reliability)
- Learning curve for Service Bus-specific concepts

Neutral:
- Must refactor existing synchronous calls to async
- Need to establish message schema conventions
```

---

## Implementation Notes

Practical details for implementing this decision:

**Steps**:
1. Step 1
2. Step 2
3. Step 3

**Timeline**: [Estimated time to implement]

**Migration Strategy**: [If replacing existing system]

**Example**:
```
Steps:
1. Provision Service Bus namespace in dev environment
2. Create domain event publishers and subscribers
3. Implement outbox pattern for reliable publishing
4. Refactor 3 key workflows to use async messaging
5. Monitor and tune performance

Timeline: 3 weeks

Migration Strategy:
- Dual-write to both sync and async for 1 week
- Gradually move consumers to event-based
- Deprecate sync endpoints after validation
```

---

## Validation

How will we know if this decision was correct?

**Success Criteria**:
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

**Review Date**: [Date to reassess this decision]

**Example**:
```
Success Criteria:
- [ ] Messages processed with <1% failure rate
- [ ] Latency acceptable (<500ms end-to-end)
- [ ] Team comfortable with Service Bus in 1 month
- [ ] Cost within budget (<$500/month for dev/staging)

Review Date: 2025-06-15 (6 months after implementation)
```

---

## References

- [Link to relevant documentation]
- [Link to comparison articles]
- [Link to vendor documentation]
- [Link to related ADRs]

**Example**:
```
- Azure Service Bus Documentation: https://docs.microsoft.com/azure/service-bus-messaging/
- RabbitMQ vs Azure Service Bus comparison: https://example.com/comparison
- Related: ADR-003 (Use Event Sourcing for Audit Trail)
- Related: ADR-012 (Outbox Pattern for Reliable Events)
```

---

## Decision Log

Track updates to this ADR:

| Date | Author | Change |
|------|--------|--------|
| 2025-01-15 | Jane Doe | Initial draft proposed |
| 2025-01-18 | Team | Decision accepted after review |
| 2025-06-15 | Jane Doe | Validation: Success criteria met |

---

## Example Complete ADR

```markdown
# ADR-005: Use Azure Service Bus for Event-Driven Messaging

Date: 2025-01-15
Status: Accepted
Deciders: Tech Lead, Backend Team, DevOps Lead
Technical Story: ARCH-42

## Context

We need asynchronous messaging to decouple services and enable event-driven 
workflows. Current HTTP-based communication creates tight coupling and makes 
it difficult to add new event consumers without modifying existing code.

Requirements:
- Reliable message delivery with retries
- Topic/subscription model for event fan-out
- Dead-letter queues for error handling
- Scale to 10,000+ messages/second
- Integration with Azure ecosystem

## Decision Drivers

- Native Azure integration (High)
- At-least-once delivery guarantees (High)
- Cost-effective for our scale (Medium)
- Topic/subscription support (High)
- Team has Azure experience (Low)

## Considered Options

### Option 1: Azure Service Bus

Pros:
- Native Azure service with excellent monitoring
- Built-in retry and dead-letter queues
- Topics and subscriptions for fan-out
- Automatic scaling

Cons:
- Azure vendor lock-in
- More expensive than self-hosted
- Premium tier required for some features

Cost: ~$500/month (Standard tier)
Effort: Low (managed service)

### Option 2: RabbitMQ (Self-Hosted on VMs)

Pros:
- No vendor lock-in
- Team has some experience
- Lower direct cost

Cons:
- Must manage infrastructure
- Manual scaling and monitoring
- Higher operational overhead
- Less integrated with Azure

Cost: ~$300/month (VM costs)
Effort: High (infrastructure management)

### Option 3: Azure Event Grid

Pros:
- Serverless, pay-per-event
- Very scalable
- Simple model

Cons:
- Limited message size (64 KB)
- No message ordering
- No dead-letter queue (requires extra setup)
- Better for simple notifications

Cost: ~$50/month
Effort: Medium

## Decision

We will use Azure Service Bus because it provides the best combination of 
reliability, Azure integration, and operational simplicity. The vendor 
lock-in is acceptable given the reduced operational burden and excellent 
monitoring integration.

## Consequences

Positive:
- Reduced operational overhead
- Built-in monitoring and alerting
- Reliable message delivery
- Easy to add new event subscribers

Negative:
- Azure vendor lock-in (mitigate: abstract behind IMessageBus interface)
- Higher cost than self-hosted (acceptable for reliability benefits)
- Team needs to learn Service Bus concepts

Neutral:
- Must refactor existing sync calls to async
- Need message schema conventions

## Implementation Notes

Steps:
1. Provision Service Bus namespace (dev, staging, prod)
2. Create domain event base classes
3. Implement outbox pattern
4. Refactor 3 key workflows to async
5. Monitor and optimize

Timeline: 3 weeks

Migration: Gradual - dual-write for 1 week, then migrate consumers

## Validation

Success Criteria:
- [✓] Messages processed with <1% failure rate
- [✓] Latency <500ms end-to-end
- [ ] Team comfortable after 1 month
- [✓] Cost within $500/month budget

Review Date: 2025-07-15

## References

- Azure Service Bus: https://docs.microsoft.com/azure/service-bus-messaging/
- Related: ADR-003 (Event Sourcing), ADR-012 (Outbox Pattern)
```

