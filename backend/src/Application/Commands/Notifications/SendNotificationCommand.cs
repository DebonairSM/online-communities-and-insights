using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Application.Commands.Notifications;

/// <summary>
/// Command to send a notification using a template.
/// </summary>
/// <param name="TemplateName">The name of the template to use.</param>
/// <param name="Recipient">The recipient's contact information.</param>
/// <param name="TemplateData">The data to substitute in the template.</param>
/// <param name="TenantId">The tenant ID for multi-tenant support.</param>
/// <param name="CorrelationId">Optional correlation ID for tracking.</param>
/// <param name="ScheduledAt">Optional scheduled send time.</param>
public record SendNotificationCommand(
    string TemplateName,
    string Recipient,
    Dictionary<string, object> TemplateData,
    Guid TenantId,
    string? CorrelationId = null,
    DateTime? ScheduledAt = null
) : ICommand<Guid>;
