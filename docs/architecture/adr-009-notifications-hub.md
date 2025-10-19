# ADR-009: Notifications Hub Architecture

## Status
**Proposed** - 2025-01-15

## Context

The Online Communities platform needs a robust notification system to deliver various types of notifications (email, SMS, push) to users across different channels. The system must be scalable, reliable, and support multiple notification providers while maintaining consistency and auditability.

## Decision

We will implement a Notifications Hub using the following architecture:

- **APIM Gateway** → **Azure Service Bus** → **Orchestrator** → **External Providers** (Twilio/SendGrid)
- Event-driven architecture with message queuing for reliability
- Template-based notification system with multi-channel support
- Idempotent message processing with dead letter queue handling

## Architecture Components

### 1. API Management Gateway
- **Purpose**: Entry point for notification requests
- **Responsibilities**: 
  - Request validation and rate limiting
  - Authentication and authorization
  - Request routing and load balancing
- **Technology**: Azure API Management

### 2. Azure Service Bus
- **Purpose**: Reliable message queuing and routing
- **Responsibilities**:
  - Message persistence and delivery guarantees
  - Topic/subscription model for message routing
  - Dead letter queue for failed messages
  - Retry policies and exponential backoff
- **Technology**: Azure Service Bus Premium

### 3. Notification Orchestrator
- **Purpose**: Central coordination of notification processing
- **Responsibilities**:
  - Template processing and personalization
  - Recipient validation and routing
  - Multi-channel delivery coordination
  - Status tracking and audit logging
- **Technology**: ASP.NET Core service with CQRS pattern

### 4. External Providers
- **Email**: SendGrid for transactional emails
- **SMS**: Twilio for SMS notifications
- **Push**: Firebase/APNs for mobile push notifications
- **Future**: Extensible for additional providers

## Implementation Details

### Message Flow
1. **Request**: Client sends notification request to APIM
2. **Validation**: APIM validates request and applies policies
3. **Publishing**: Request published to Service Bus topic
4. **Processing**: Orchestrator processes message from subscription
5. **Delivery**: External provider delivers notification
6. **Tracking**: Status updated and audit logged

### Data Models

#### NotificationEvent
```csharp
public class NotificationEvent : BaseEntity
{
    public Guid TenantId { get; set; }
    public string NotificationType { get; set; }
    public Guid? TemplateId { get; set; }
    public string Recipient { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public NotificationStatus Status { get; set; }
    public int RetryCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? CorrelationId { get; set; }
}
```

#### NotificationTemplate
```csharp
public class NotificationTemplate : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string NotificationType { get; set; }
    public string SubjectTemplate { get; set; }
    public string? HtmlBodyTemplate { get; set; }
    public string? TextBodyTemplate { get; set; }
    public bool IsActive { get; set; }
}
```

### Service Interfaces

#### IMessageBusService
```csharp
public interface IMessageBusService
{
    Task<string> PublishAsync<T>(string topicName, T message, string? correlationId = null);
    Task<string> PublishNotificationAsync(NotificationEvent notificationEvent);
    Task<string> SendToDeadLetterQueueAsync<T>(string originalTopicName, T message, string reason);
}
```

#### INotificationService
```csharp
public interface INotificationService
{
    Task<Guid> SendNotificationAsync(string templateName, string recipient, 
        Dictionary<string, object> templateData, Guid tenantId, string? correlationId = null);
    Task<Guid> ScheduleNotificationAsync(string templateName, string recipient, 
        Dictionary<string, object> templateData, Guid tenantId, DateTime scheduledAt);
    Task<bool> CancelNotificationAsync(Guid notificationId);
}
```

## Configuration

### Service Bus Configuration
```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...",
    "NotificationsTopicName": "notifications",
    "NotificationsSubscriptionName": "email-processor",
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 30,
    "MessageTimeToLiveMinutes": 60
  }
}
```

### Email Configuration
```json
{
  "Email": {
    "ApiKey": "your-sendgrid-api-key",
    "FromAddress": "noreply@onlinecommunities.com",
    "FromName": "Online Communities",
    "MaxRetryAttempts": 3,
    "EnableTracking": true
  }
}
```

## Benefits

### Reliability
- **Message Persistence**: Service Bus ensures message durability
- **Retry Logic**: Automatic retry with exponential backoff
- **Dead Letter Queues**: Failed messages captured for manual review
- **Idempotency**: Duplicate message handling

### Scalability
- **Horizontal Scaling**: Multiple orchestrator instances
- **Load Distribution**: Service Bus topic/subscription model
- **Provider Abstraction**: Easy addition of new notification channels
- **Rate Limiting**: APIM provides request throttling

### Maintainability
- **Template System**: Centralized notification templates
- **CQRS Pattern**: Clear separation of concerns
- **Audit Trail**: Complete notification history
- **Monitoring**: Built-in Service Bus monitoring

### Flexibility
- **Multi-Channel**: Support for email, SMS, push notifications
- **Scheduling**: Future notification scheduling
- **Personalization**: Template-based content customization
- **Tenant Isolation**: Multi-tenant support

## Trade-offs

### Complexity
- **Additional Infrastructure**: Service Bus and APIM required
- **Message Processing**: Asynchronous processing complexity
- **Error Handling**: More complex error scenarios
- **Monitoring**: Additional monitoring requirements

### Cost
- **Service Bus**: Premium tier for advanced features
- **APIM**: Gateway costs for request processing
- **External Providers**: Per-message costs for delivery
- **Storage**: Message persistence and audit data

### Latency
- **Asynchronous**: Notifications not immediately delivered
- **Queue Processing**: Additional processing time
- **Provider Latency**: External provider response times
- **Retry Delays**: Failed message retry intervals

## Alternatives Considered

### 1. Direct API Calls
- **Pros**: Simple implementation, immediate delivery
- **Cons**: No reliability guarantees, difficult to scale, no audit trail

### 2. Database Queue
- **Pros**: Simple, uses existing infrastructure
- **Cons**: Poor performance, no built-in retry logic, scaling issues

### 3. Azure Event Grid
- **Pros**: Serverless, pay-per-event
- **Cons**: Limited message size, no dead letter queues, less control

### 4. Azure Functions
- **Pros**: Serverless, automatic scaling
- **Cons**: Cold start latency, limited execution time, vendor lock-in

## Implementation Plan

### Phase 1: Core Infrastructure
- [ ] Set up Azure Service Bus namespace and topics
- [ ] Implement ServiceBusService with basic publishing
- [ ] Create NotificationEvent and NotificationTemplate entities
- [ ] Implement basic INotificationService interface

### Phase 2: Email Integration
- [ ] Integrate SendGrid service
- [ ] Implement template processing
- [ ] Add email validation and error handling
- [ ] Create email-specific CQRS commands

### Phase 3: Orchestration
- [ ] Implement notification orchestrator
- [ ] Add message processing logic
- [ ] Implement retry and dead letter handling
- [ ] Add audit logging and status tracking

### Phase 4: APIM Integration
- [ ] Set up Azure API Management
- [ ] Create notification API endpoints
- [ ] Implement rate limiting and policies
- [ ] Add authentication and authorization

### Phase 5: Advanced Features
- [ ] Add SMS integration (Twilio)
- [ ] Implement push notifications
- [ ] Add notification scheduling
- [ ] Implement advanced monitoring and alerting

## Monitoring and Observability

### Key Metrics
- **Message Throughput**: Messages processed per minute
- **Success Rate**: Percentage of successful deliveries
- **Latency**: End-to-end notification delivery time
- **Error Rate**: Failed message percentage
- **Queue Depth**: Pending messages in Service Bus

### Alerts
- **High Error Rate**: >5% failure rate
- **Queue Backlog**: >1000 pending messages
- **Provider Downtime**: External provider failures
- **Message Age**: Messages older than 1 hour

### Dashboards
- **Notification Volume**: Daily/weekly notification counts
- **Channel Performance**: Success rates by notification type
- **Provider Health**: External provider status and performance
- **System Health**: Service Bus and orchestrator health

## Security Considerations

### Data Protection
- **Encryption**: Messages encrypted in transit and at rest
- **PII Handling**: Personal information protection
- **Audit Logging**: Complete audit trail for compliance
- **Access Control**: Role-based access to notification data

### Rate Limiting
- **Per-User Limits**: Prevent notification spam
- **Per-Tenant Limits**: Tenant-specific rate limiting
- **Global Limits**: System-wide rate limiting
- **Burst Handling**: Temporary rate limit increases

### Provider Security
- **API Key Management**: Secure storage of provider credentials
- **Webhook Validation**: Verify webhook authenticity
- **IP Whitelisting**: Restrict provider access
- **Certificate Validation**: SSL/TLS certificate verification

## Compliance

### GDPR
- **Data Minimization**: Only collect necessary data
- **Right to Erasure**: Delete user notification data
- **Data Portability**: Export user notification history
- **Consent Management**: Track user notification preferences

### SOC 2
- **Access Controls**: Role-based access to notification system
- **Audit Logging**: Complete audit trail
- **Data Encryption**: Encryption in transit and at rest
- **Incident Response**: Notification system incident procedures

## Future Considerations

### Scalability
- **Multi-Region**: Deploy across multiple Azure regions
- **Auto-Scaling**: Dynamic scaling based on load
- **Caching**: Redis caching for templates and configuration
- **CDN**: Content delivery for notification assets

### Features
- **A/B Testing**: Template and content testing
- **Analytics**: Notification engagement analytics
- **Personalization**: AI-driven content personalization
- **Multi-Language**: Internationalization support

### Integration
- **CRM Integration**: Customer relationship management
- **Marketing Automation**: Campaign management
- **Analytics Platforms**: Business intelligence integration
- **Third-Party Services**: Additional notification providers

## Conclusion

The Notifications Hub architecture provides a robust, scalable, and maintainable solution for notification delivery. While it introduces additional complexity and cost, the benefits of reliability, scalability, and flexibility justify the investment. The phased implementation approach allows for incremental delivery and validation of the solution.

## References

- [Azure Service Bus Documentation](https://docs.microsoft.com/en-us/azure/service-bus/)
- [Azure API Management Documentation](https://docs.microsoft.com/en-us/azure/api-management/)
- [SendGrid API Documentation](https://docs.sendgrid.com/)
- [Twilio API Documentation](https://www.twilio.com/docs)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
