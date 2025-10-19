using System.ComponentModel.DataAnnotations;

namespace OnlineCommunities.Infrastructure.Governance;

/// <summary>
/// Configuration options for certificate management and rotation.
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// The configuration section name for certificate options.
    /// </summary>
    public const string SectionName = "CertificateManagement";

    /// <summary>
    /// The number of days before expiry to send alerts.
    /// </summary>
    [Range(1, 365)]
    public int ExpiryAlertDays { get; set; } = 30;

    /// <summary>
    /// The number of days before expiry to start rotation.
    /// </summary>
    [Range(1, 365)]
    public int RotationStartDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable automated rotation.
    /// </summary>
    public bool EnableAutomatedRotation { get; set; } = true;

    /// <summary>
    /// The Azure Key Vault name for certificate storage.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string KeyVaultName { get; set; } = string.Empty;

    /// <summary>
    /// The Azure Key Vault URL.
    /// </summary>
    [Url]
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// The notification email addresses for certificate alerts.
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();

    /// <summary>
    /// The Teams webhook URL for notifications.
    /// </summary>
    [Url]
    public string? TeamsWebhookUrl { get; set; }

    /// <summary>
    /// The PagerDuty integration key for critical alerts.
    /// </summary>
    [StringLength(100)]
    public string? PagerDutyIntegrationKey { get; set; }

    /// <summary>
    /// The maximum number of rotation retry attempts.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The delay between rotation retry attempts in seconds.
    /// </summary>
    [Range(60, 3600)]
    public int RetryDelaySeconds { get; set; } = 300;

    /// <summary>
    /// Whether to validate certificates after rotation.
    /// </summary>
    public bool ValidateAfterRotation { get; set; } = true;

    /// <summary>
    /// The timeout for certificate validation in seconds.
    /// </summary>
    [Range(30, 300)]
    public int ValidationTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to enable certificate health monitoring.
    /// </summary>
    public bool EnableHealthMonitoring { get; set; } = true;

    /// <summary>
    /// The interval for health checks in minutes.
    /// </summary>
    [Range(5, 1440)]
    public int HealthCheckIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to enable SSL handshake monitoring.
    /// </summary>
    public bool EnableSslHandshakeMonitoring { get; set; } = true;

    /// <summary>
    /// The timeout for SSL handshake tests in seconds.
    /// </summary>
    [Range(5, 60)]
    public int SslHandshakeTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable OCSP (Online Certificate Status Protocol) checking.
    /// </summary>
    public bool EnableOcspChecking { get; set; } = true;

    /// <summary>
    /// The timeout for OCSP requests in seconds.
    /// </summary>
    [Range(5, 60)]
    public int OcspTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to enable CRL (Certificate Revocation List) checking.
    /// </summary>
    public bool EnableCrlChecking { get; set; } = true;

    /// <summary>
    /// The timeout for CRL requests in seconds.
    /// </summary>
    [Range(5, 60)]
    public int CrlTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Whether to enable certificate chain validation.
    /// </summary>
    public bool EnableChainValidation { get; set; } = true;

    /// <summary>
    /// The maximum chain length for validation.
    /// </summary>
    [Range(1, 10)]
    public int MaxChainLength { get; set; } = 5;

    /// <summary>
    /// Whether to enable domain validation.
    /// </summary>
    public bool EnableDomainValidation { get; set; } = true;

    /// <summary>
    /// The list of trusted certificate authorities.
    /// </summary>
    public List<string> TrustedCertificateAuthorities { get; set; } = new();

    /// <summary>
    /// Whether to enable certificate backup before rotation.
    /// </summary>
    public bool EnableBackupBeforeRotation { get; set; } = true;

    /// <summary>
    /// The backup retention period in days.
    /// </summary>
    [Range(1, 365)]
    public int BackupRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable certificate audit logging.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// The log retention period in days.
    /// </summary>
    [Range(1, 2555)] // 7 years
    public int LogRetentionDays { get; set; } = 90;

    /// <summary>
    /// Whether to enable compliance reporting.
    /// </summary>
    public bool EnableComplianceReporting { get; set; } = true;

    /// <summary>
    /// The compliance report generation interval in days.
    /// </summary>
    [Range(1, 365)]
    public int ComplianceReportIntervalDays { get; set; } = 30;

    /// <summary>
    /// The list of compliance standards to report on.
    /// </summary>
    public List<string> ComplianceStandards { get; set; } = new() { "GDPR", "SOC2", "ISO27001" };

    /// <summary>
    /// Whether to enable emergency rotation procedures.
    /// </summary>
    public bool EnableEmergencyRotation { get; set; } = true;

    /// <summary>
    /// The emergency rotation notification list.
    /// </summary>
    public List<string> EmergencyNotificationEmails { get; set; } = new();

    /// <summary>
    /// Whether to enable certificate performance monitoring.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// The performance monitoring threshold in milliseconds.
    /// </summary>
    [Range(100, 10000)]
    public int PerformanceThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to enable certificate usage analytics.
    /// </summary>
    public bool EnableUsageAnalytics { get; set; } = true;

    /// <summary>
    /// The analytics data retention period in days.
    /// </summary>
    [Range(1, 365)]
    public int AnalyticsRetentionDays { get; set; } = 90;
}
