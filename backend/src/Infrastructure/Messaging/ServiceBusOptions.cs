using System.ComponentModel.DataAnnotations;

namespace OnlineCommunities.Infrastructure.Messaging;

/// <summary>
/// Configuration options for Azure Service Bus integration.
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// The configuration section name for Service Bus options.
    /// </summary>
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// The Service Bus connection string.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The default topic name for notifications.
    /// </summary>
    public string NotificationsTopicName { get; set; } = "notifications";

    /// <summary>
    /// The default subscription name for notifications.
    /// </summary>
    public string NotificationsSubscriptionName { get; set; } = "email-processor";

    /// <summary>
    /// The dead letter queue topic name pattern.
    /// </summary>
    public string DeadLetterTopicPattern { get; set; } = "{0}-dlq";

    /// <summary>
    /// The maximum number of retry attempts for failed messages.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The retry delay in seconds between attempts.
    /// </summary>
    [Range(1, 300)]
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// The message time-to-live in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int MessageTimeToLiveMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to enable duplicate detection.
    /// </summary>
    public bool EnableDuplicateDetection { get; set; } = true;

    /// <summary>
    /// The duplicate detection window in minutes.
    /// </summary>
    [Range(1, 60)]
    public int DuplicateDetectionWindowMinutes { get; set; } = 10;

    /// <summary>
    /// Whether to enable session support for ordered processing.
    /// </summary>
    public bool EnableSessions { get; set; } = false;

    /// <summary>
    /// The maximum delivery count before sending to dead letter queue.
    /// </summary>
    [Range(1, 20)]
    public int MaxDeliveryCount { get; set; } = 5;

    /// <summary>
    /// Whether to enable auto-delete on idle (for development only).
    /// </summary>
    public bool AutoDeleteOnIdle { get; set; } = false;

    /// <summary>
    /// The auto-delete idle time in minutes.
    /// </summary>
    [Range(5, 10080)]
    public int AutoDeleteIdleTimeMinutes { get; set; } = 60;
}
