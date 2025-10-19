using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Entities.Notifications;

/// <summary>
/// Represents a notification event that can be processed by the notification system.
/// This entity tracks notification events for audit and retry purposes.
/// </summary>
public class NotificationEvent : BaseEntity
{
    /// <summary>
    /// The tenant ID this notification belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The type of notification (e.g., "email", "sms", "push").
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// The template ID used for this notification.
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// The recipient's email address or phone number.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// The subject line for the notification.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The content/body of the notification.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata for the notification in JSON format.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// The current status of the notification.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// The number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// The maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When the notification was scheduled to be sent.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// When the notification was actually sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Any error message from failed attempts.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The correlation ID for tracking related notifications.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The message ID from the message bus for tracking.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Navigation property to the notification template.
    /// </summary>
    public virtual NotificationTemplate? Template { get; set; }
}

/// <summary>
/// Enumeration of notification statuses.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is pending processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Notification was sent successfully.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Notification failed to send.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Notification was cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Notification exceeded maximum retry attempts.
    /// </summary>
    DeadLetter = 5
}
