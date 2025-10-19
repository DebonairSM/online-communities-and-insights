using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Entities.CDM;

/// <summary>
/// Base entity for Common Data Model (CDM) data ingestion.
/// Provides standardized structure for external data integration.
/// </summary>
public class CommonDataEntity : BaseEntity
{
    /// <summary>
    /// The tenant ID this data belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The source system identifier where this data originated.
    /// </summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>
    /// The type of data entity (e.g., "user", "order", "product").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The external ID from the source system.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// The version of the data schema.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// The raw data payload in JSON format.
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// The processed/normalized data in JSON format.
    /// </summary>
    public string? ProcessedData { get; set; }

    /// <summary>
    /// The ingestion status.
    /// </summary>
    public IngestionStatus Status { get; set; } = IngestionStatus.Pending;

    /// <summary>
    /// When the data was received from the source system.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the data was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// The processing duration in milliseconds.
    /// </summary>
    public long? ProcessingDurationMs { get; set; }

    /// <summary>
    /// Any error message from failed processing.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The number of processing attempts made.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// The maximum number of processing attempts allowed.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Additional metadata about the data in JSON format.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// The data quality score (0-100).
    /// </summary>
    public int? DataQualityScore { get; set; }

    /// <summary>
    /// Whether this data has been validated against the schema.
    /// </summary>
    public bool IsValidated { get; set; } = false;

    /// <summary>
    /// The validation errors in JSON format.
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// The correlation ID for tracking related data.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The batch ID for grouping related data ingestion.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// Whether this data should be processed immediately or queued.
    /// </summary>
    public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;

    /// <summary>
    /// The hash of the raw data for duplicate detection.
    /// </summary>
    public string? DataHash { get; set; }

    /// <summary>
    /// When the data expires and should be removed.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this data has been archived.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the data was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
}

/// <summary>
/// Enumeration of data ingestion statuses.
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// Data is pending processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Data is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Data was processed successfully.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Data processing failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Data was skipped due to validation errors.
    /// </summary>
    Skipped = 4,

    /// <summary>
    /// Data was rejected due to quality issues.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// Data processing was cancelled.
    /// </summary>
    Cancelled = 6
}

/// <summary>
/// Enumeration of processing priorities.
/// </summary>
public enum ProcessingPriority
{
    /// <summary>
    /// Low priority processing.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority processing.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority processing.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority processing.
    /// </summary>
    Critical = 3
}
