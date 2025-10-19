using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Entities.Notifications;

/// <summary>
/// Represents an email template that can be used for notifications.
/// Templates support variable substitution and multiple formats.
/// </summary>
public class NotificationTemplate : BaseEntity
{
    /// <summary>
    /// The tenant ID this template belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The unique name/identifier for this template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this template is used for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of notification this template is for (e.g., "email", "sms").
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// The subject line template with variable placeholders.
    /// </summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// The HTML body template with variable placeholders.
    /// </summary>
    public string? HtmlBodyTemplate { get; set; }

    /// <summary>
    /// The plain text body template with variable placeholders.
    /// </summary>
    public string? TextBodyTemplate { get; set; }

    /// <summary>
    /// The sender's email address or display name.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// The sender's display name.
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Whether this template is active and can be used.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The version of this template for tracking changes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The language/culture this template is for.
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Additional metadata for the template in JSON format.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When this template was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// The number of times this template has been used.
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Navigation property to notification events using this template.
    /// </summary>
    public virtual ICollection<NotificationEvent> NotificationEvents { get; set; } = new List<NotificationEvent>();
}
