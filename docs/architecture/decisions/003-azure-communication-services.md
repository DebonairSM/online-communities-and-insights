# ADR-003: Use Azure Communication Services for Email

**Date**: 2025-01-15
**Status**: Accepted
**Deciders**: Platform Developer
**Technical Story**: Email notifications for research activities

---

## Context

The Insight Community Platform needs to send transactional emails for:
- User registration and email verification
- Password reset requests
- Research activity notifications (new survey, deadline reminders)
- Moderation alerts (content flagged, submission approved)
- Incentive notifications (points awarded, rewards available)
- Weekly digest emails (activity summaries)

Requirements:
- Reliable delivery with retry logic
- Template-based emails with personalization
- Track delivery status
- Handle high volume (thousands of emails/day potential)
- Support both transactional and marketing emails
- Maintain sender reputation

Current situation: Starting from scratch, need email service that integrates with Azure infrastructure.

---

## Decision Drivers

- **Azure integration**: Works seamlessly with other Azure services (High priority)
- **Reliability**: High deliverability rate with automatic retries (High)
- **Cost**: Reasonable pricing for research community scale (High)
- **Simplicity**: Easy to implement and maintain for solo developer (High)
- **Templates**: Support for email templates (Medium)
- **Analytics**: Delivery tracking and reporting (Medium)
- **Scalability**: Handles growth without manual intervention (Medium)

---

## Considered Options

### Option 1: Azure Communication Services (Email)

**Description**: Azure's native email service (newer offering, GA 2023)

**Pros**:
- Native Azure integration (same tenant, IAM, monitoring)
- Pay-per-message pricing (no monthly minimums)
- Managed infrastructure (no servers to maintain)
- Automatic retry logic
- Delivery status tracking
- Custom domain support
- Application Insights integration
- SMTP or SDK options
- No credit card required to start

**Cons**:
- Newer service (less mature than SendGrid)
- Limited templating features compared to SendGrid
- Must build custom template system
- Fewer pre-built integrations

**Cost**: 
- Free tier: 100 emails/month
- $0.25 per 1,000 emails beyond free tier
- ~$25/month for 100,000 emails

**Implementation Effort**: Low-Medium

---

### Option 2: SendGrid (Twilio)

**Description**: Popular email service with rich features

**Pros**:
- Mature platform with proven reliability
- Rich templating engine (dynamic content, personalization)
- Marketing email features (unsubscribe management)
- Email validation and list management
- Detailed analytics dashboard
- A/B testing built-in
- Many integration examples

**Cons**:
- External service outside Azure (another vendor)
- Monthly minimum even for low volume
- More expensive for low-moderate volume
- Additional vendor to manage
- More features than needed initially

**Cost**:
- Free tier: 100 emails/day (3,000/month)
- Essentials: $20/month for 50,000 emails
- Pro: $90/month for 100,000 emails

**Implementation Effort**: Low

---

### Option 3: Microsoft 365 / Exchange Online

**Description**: Use enterprise email service

**Pros**:
- May already have M365 licenses
- Familiar interface
- Full email capabilities

**Cons**:
- Not designed for transactional emails
- Rate limits per mailbox
- Difficult to programmatically send
- Poor deliverability for automated emails
- Expensive for this use case

**Cost**: $6-12 per user/month

**Implementation Effort**: Medium-High

---

### Option 4: Amazon SES

**Description**: AWS email service

**Pros**:
- Very cheap ($0.10 per 1,000 emails)
- Highly scalable
- Mature service

**Cons**:
- Outside Azure ecosystem
- Requires AWS account and billing
- Less integrated with our stack
- Another cloud vendor to manage

**Cost**: $0.10 per 1,000 emails

**Implementation Effort**: Medium

---

## Decision

**We will use Azure Communication Services (Email)** because it provides native Azure integration at competitive pricing with sufficient features for our transactional email needs.

**Rationale**:
1. **Azure-first**: Staying within Azure ecosystem simplifies operations and monitoring
2. **Cost-effective**: Pay-per-use pricing works well for variable volume
3. **Right-sized**: Has what we need without excessive features
4. **Simple operations**: One less external vendor to manage
5. **Integration**: Uses same Azure AD, Key Vault, Application Insights as rest of platform

SendGrid would be a valid choice but adds vendor complexity. We can build a simple template system and add SendGrid later if marketing email features are needed. For research notifications, simplicity and reliability matter more than advanced marketing features.

---

## Consequences

### Positive
- Unified Azure billing and monitoring
- No monthly minimum (good for low-volume start)
- Easy to implement with Azure SDK
- Automatic integration with Application Insights
- Can use Managed Identity for authentication
- Scales automatically as email volume grows

### Negative
- Must build custom email template system (mitigate: use Razor templates or similar)
- Less mature than SendGrid (acceptable for transactional emails)
- Fewer built-in marketing features (not needed initially)
- Limited analytics compared to SendGrid (Azure Monitor fills gap)

### Neutral
- Need to configure custom domain for better deliverability
- Must implement unsubscribe management manually (required for GDPR)
- Template versioning on us (gives control)

---

## Implementation Notes

**Steps**:
1. Create Azure Communication Services resource
2. Configure custom domain (e.g., noreply@platform.com)
3. Verify domain with DNS records
4. Create email template system (Razor templates)
5. Implement NotificationService with email channel
6. Set up retry logic and error handling
7. Configure Application Insights tracking

**Timeline**: 1 week for basic implementation

**Template System**:
```csharp
public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object model);
}

public class RazorEmailTemplateService : IEmailTemplateService
{
    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        // Load template from file or database
        var template = await LoadTemplateAsync(templateName);
        
        // Render with Razor or similar
        return await RazorEngine.RenderAsync(template, model);
    }
}
```

**Email Service**:
```csharp
public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly IEmailTemplateService _templateService;
    
    public async Task SendEmailAsync(string to, string subject, string templateName, object model)
    {
        var htmlBody = await _templateService.RenderTemplateAsync(templateName, model);
        
        var emailMessage = new EmailMessage(
            senderAddress: "noreply@platform.com",
            recipientAddress: to,
            content: new EmailContent(subject)
            {
                Html = htmlBody
            }
        );
        
        var sendOperation = await _emailClient.SendAsync(
            WaitUntil.Started,
            emailMessage
        );
        
        // Track in Application Insights
        _logger.LogInformation("Email sent to {To}, MessageId: {MessageId}", 
            to, sendOperation.Id);
    }
}
```

---

## Validation

**Success Criteria**:
- [ ] Email deliverability rate >95%
- [ ] Emails sent within 1 minute of trigger
- [ ] Template rendering works correctly
- [ ] Unsubscribe links functional
- [ ] Cost <$50/month for first 6 months
- [ ] No bounces due to configuration issues

**Review Date**: 2025-07-15 (6 months after implementation)

**Metrics to Track**:
- Delivery rate (sent vs delivered)
- Bounce rate (hard and soft)
- Average send latency
- Monthly cost
- Template rendering errors

---

## Migration Path

If we need advanced features later:
1. **Option 1**: Keep Azure Communication Services, add SendGrid for marketing emails (two services)
2. **Option 2**: Migrate to SendGrid completely if marketing features become critical
3. **Option 3**: Build more advanced features on Azure Communication Services

Migration would be straightforward due to IEmailService abstraction.

---

## References

- [Azure Communication Services Email Documentation](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview)
- [Email SDK Quickstart](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email)
- [Custom Domain Setup](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/add-custom-verified-domains)
- Related: `contexts/backend-architecture.md` - Notification Service section
- Related: Will create notification template structure

---

## Decision Log

| Date | Author | Change |
|------|--------|--------|
| 2025-01-15 | Platform Developer | Decision accepted, chosen over SendGrid |

