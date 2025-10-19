using System.ComponentModel.DataAnnotations;

namespace OnlineCommunities.Infrastructure.Integrations.Email;

/// <summary>
/// Configuration options for email service integration.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// The configuration section name for email options.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// The SendGrid API key for authentication.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The default sender email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// The default sender display name.
    /// </summary>
    public string FromName { get; set; } = "Online Communities";

    /// <summary>
    /// The SendGrid API base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.sendgrid.com/v3/";

    /// <summary>
    /// The timeout for API requests in seconds.
    /// </summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// The maximum number of retry attempts for failed requests.
    /// </summary>
    [Range(0, 5)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The delay between retry attempts in seconds.
    /// </summary>
    [Range(1, 60)]
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Whether to enable email validation using SendGrid's validation service.
    /// </summary>
    public bool EnableEmailValidation { get; set; } = true;

    /// <summary>
    /// The maximum number of recipients for bulk email operations.
    /// </summary>
    [Range(1, 10000)]
    public int MaxBulkRecipients { get; set; } = 1000;

    /// <summary>
    /// Whether to track email opens and clicks.
    /// </summary>
    public bool EnableTracking { get; set; } = true;

    /// <summary>
    /// The webhook URL for tracking events.
    /// </summary>
    public string? TrackingWebhookUrl { get; set; }

    /// <summary>
    /// Whether to enable sandbox mode for testing.
    /// </summary>
    public bool SandboxMode { get; set; } = false;

    /// <summary>
    /// The default template ID for system notifications.
    /// </summary>
    public string? DefaultTemplateId { get; set; }

    /// <summary>
    /// The default language for email templates.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    /// Whether to include unsubscribe links in emails.
    /// </summary>
    public bool IncludeUnsubscribeLink { get; set; } = true;

    /// <summary>
    /// The unsubscribe URL for email recipients.
    /// </summary>
    public string? UnsubscribeUrl { get; set; }

    /// <summary>
    /// The reply-to email address for responses.
    /// </summary>
    [EmailAddress]
    public string? ReplyToAddress { get; set; }

    /// <summary>
    /// The reply-to display name.
    /// </summary>
    public string? ReplyToName { get; set; }

    /// <summary>
    /// Whether to enable click tracking.
    /// </summary>
    public bool EnableClickTracking { get; set; } = true;

    /// <summary>
    /// Whether to enable open tracking.
    /// </summary>
    public bool EnableOpenTracking { get; set; } = true;

    /// <summary>
    /// The custom headers to include with all emails.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// The rate limit for API requests per minute.
    /// </summary>
    [Range(1, 10000)]
    public int RateLimitPerMinute { get; set; } = 100;
}
