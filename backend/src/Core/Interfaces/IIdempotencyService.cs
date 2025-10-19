using OnlineCommunities.Core.Entities.Processing;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Service for ensuring idempotent message processing.
/// Prevents duplicate processing of messages and tracks processing history.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a message has already been processed successfully.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message has been processed successfully, false otherwise.</returns>
    Task<bool> IsMessageProcessedAsync(string messageId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the processing record for a message.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processing record or null if not found.</returns>
    Task<MessageProcessingRecord?> GetProcessingRecordAsync(string messageId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new processing record for a message.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="messageType">The type of message.</param>
    /// <param name="sourceTopic">The source topic or queue.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="messageMetadata">Optional message metadata.</param>
    /// <param name="messageHash">Optional message content hash.</param>
    /// <param name="priority">The message priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created processing record.</returns>
    Task<MessageProcessingRecord> CreateProcessingRecordAsync(
        string messageId,
        string messageType,
        string sourceTopic,
        Guid tenantId,
        string? messageMetadata = null,
        string? messageHash = null,
        MessagePriority priority = MessagePriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the processing status of a message.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="status">The new processing status.</param>
    /// <param name="errorMessage">Optional error message for failed processing.</param>
    /// <param name="exceptionDetails">Optional exception details in JSON format.</param>
    /// <param name="processingResult">Optional processing result data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false if the record was not found.</returns>
    Task<bool> UpdateProcessingStatusAsync(
        string messageId,
        Guid tenantId,
        ProcessingStatus status,
        string? errorMessage = null,
        string? exceptionDetails = null,
        string? processingResult = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as dead lettered.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="reason">The reason for dead lettering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false if the record was not found.</returns>
    Task<bool> MarkAsDeadLetteredAsync(
        string messageId,
        Guid tenantId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages that are ready for retry.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="maxCount">The maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of processing records ready for retry.</returns>
    Task<IEnumerable<MessageProcessingRecord>> GetMessagesReadyForRetryAsync(
        Guid tenantId,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead lettered messages for manual review.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of dead lettered processing records.</returns>
    Task<IEnumerable<MessageProcessingRecord>> GetDeadLetteredMessagesAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the next retry time based on the attempt count and retry policy.
    /// </summary>
    /// <param name="attemptCount">The current attempt count.</param>
    /// <param name="baseDelaySeconds">The base delay in seconds.</param>
    /// <param name="maxDelaySeconds">The maximum delay in seconds.</param>
    /// <returns>The next retry time.</returns>
    DateTime CalculateNextRetryTime(int attemptCount, int baseDelaySeconds = 30, int maxDelaySeconds = 3600);

    /// <summary>
    /// Cleans up old processing records based on retention policy.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="retentionDays">The number of days to retain records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records cleaned up.</returns>
    Task<int> CleanupOldRecordsAsync(
        Guid tenantId,
        int retentionDays = 30,
        CancellationToken cancellationToken = default);
}
