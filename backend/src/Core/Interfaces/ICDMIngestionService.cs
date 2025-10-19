using OnlineCommunities.Core.Entities.CDM;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Service for ingesting Common Data Model (CDM) data from external systems.
/// Provides smart ingestion with validation, transformation, and error handling.
/// </summary>
public interface ICDMIngestionService
{
    /// <summary>
    /// Ingests a single data entity from an external system.
    /// </summary>
    /// <param name="sourceSystem">The source system identifier.</param>
    /// <param name="entityType">The type of entity being ingested.</param>
    /// <param name="externalId">The external ID of the entity.</param>
    /// <param name="rawData">The raw data payload.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="batchId">Optional batch ID for grouping related data.</param>
    /// <param name="priority">The processing priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ingested data entity ID.</returns>
    Task<Guid> IngestDataAsync(
        string sourceSystem,
        string entityType,
        string externalId,
        string rawData,
        Guid tenantId,
        string? correlationId = null,
        string? batchId = null,
        ProcessingPriority priority = ProcessingPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests multiple data entities in a batch.
    /// </summary>
    /// <param name="batchData">The batch data to ingest.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="batchId">The batch ID for grouping related data.</param>
    /// <param name="priority">The processing priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entities successfully ingested.</returns>
    Task<int> IngestBatchAsync(
        IEnumerable<BatchDataItem> batchData,
        Guid tenantId,
        string batchId,
        ProcessingPriority priority = ProcessingPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates data against the CDM schema.
    /// </summary>
    /// <param name="entityType">The type of entity to validate.</param>
    /// <param name="rawData">The raw data to validate.</param>
    /// <param name="schemaVersion">The schema version to validate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateDataAsync(
        string entityType,
        string rawData,
        string schemaVersion = "1.0",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms raw data into the standardized CDM format.
    /// </summary>
    /// <param name="entityType">The type of entity to transform.</param>
    /// <param name="rawData">The raw data to transform.</param>
    /// <param name="sourceSystem">The source system identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transformed data.</returns>
    Task<string> TransformDataAsync(
        string entityType,
        string rawData,
        string sourceSystem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the ingestion status of a data entity.
    /// </summary>
    /// <param name="entityId">The data entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The data entity or null if not found.</returns>
    Task<CommonDataEntity?> GetIngestionStatusAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data entities by status and optional filters.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="sourceSystem">Optional source system filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of data entities.</returns>
    Task<IEnumerable<CommonDataEntity>> GetDataEntitiesAsync(
        Guid tenantId,
        IngestionStatus? status = null,
        string? entityType = null,
        string? sourceSystem = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed data ingestion.
    /// </summary>
    /// <param name="entityId">The data entity ID to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if retry was initiated, false if not found or max attempts exceeded.</returns>
    Task<bool> RetryIngestionAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ingestion statistics for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ingestion statistics.</returns>
    Task<IngestionStats> GetIngestionStatsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old ingested data based on retention policy.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="retentionDays">The number of days to retain data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records cleaned up.</returns>
    Task<int> CleanupOldDataAsync(
        Guid tenantId,
        int retentionDays = 90,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a batch data item for ingestion.
/// </summary>
public class BatchDataItem
{
    /// <summary>
    /// The source system identifier.
    /// </summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>
    /// The type of entity being ingested.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The external ID of the entity.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// The raw data payload.
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// Optional correlation ID for tracking.
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Represents the result of data validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the data is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The validation errors if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// The validation warnings if any.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// The data quality score (0-100).
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// The schema version used for validation.
    /// </summary>
    public string SchemaVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents ingestion statistics.
/// </summary>
public class IngestionStats
{
    /// <summary>
    /// The total number of entities ingested.
    /// </summary>
    public int TotalEntities { get; set; }

    /// <summary>
    /// The number of entities by status.
    /// </summary>
    public Dictionary<IngestionStatus, int> EntitiesByStatus { get; set; } = new();

    /// <summary>
    /// The number of entities by type.
    /// </summary>
    public Dictionary<string, int> EntitiesByType { get; set; } = new();

    /// <summary>
    /// The number of entities by source system.
    /// </summary>
    public Dictionary<string, int> EntitiesBySource { get; set; } = new();

    /// <summary>
    /// The average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// The average data quality score.
    /// </summary>
    public double AverageQualityScore { get; set; }

    /// <summary>
    /// The timestamp of the first ingestion.
    /// </summary>
    public DateTime? FirstIngestionAt { get; set; }

    /// <summary>
    /// The timestamp of the last ingestion.
    /// </summary>
    public DateTime? LastIngestionAt { get; set; }
}
