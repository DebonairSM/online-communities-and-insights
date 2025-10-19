using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Entities.Processing;

/// <summary>
/// Tracks message processing for idempotency and audit purposes.
/// Ensures messages are not processed multiple times and provides processing history.
/// </summary>
public class MessageProcessingRecord : BaseEntity
{
    /// <summary>
    /// The tenant ID this processing record belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The unique identifier for the message (correlation ID or message ID).
    /// Used for idempotency checking.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The type of message being processed.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The topic or queue name where the message originated.
    /// </summary>
    public string SourceTopic { get; set; } = string.Empty;

    /// <summary>
    /// The current processing status.
    /// </summary>
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    /// <summary>
    /// The number of processing attempts made.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// The maximum number of processing attempts allowed.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// When the message was first received.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When processing started.
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// When processing completed (successfully or with failure).
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; }

    /// <summary>
    /// The processing duration in milliseconds.
    /// </summary>
    public long? ProcessingDurationMs { get; set; }

    /// <summary>
    /// Any error message from failed processing attempts.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The exception details in JSON format for debugging.
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Additional metadata about the message in JSON format.
    /// </summary>
    public string? MessageMetadata { get; set; }

    /// <summary>
    /// The hash of the message content for duplicate detection.
    /// </summary>
    public string? MessageHash { get; set; }

    /// <summary>
    /// Whether this message has been sent to the dead letter queue.
    /// </summary>
    public bool IsDeadLettered { get; set; } = false;

    /// <summary>
    /// When the message was sent to the dead letter queue.
    /// </summary>
    public DateTime? DeadLetteredAt { get; set; }

    /// <summary>
    /// The reason for sending to dead letter queue.
    /// </summary>
    public string? DeadLetterReason { get; set; }

    /// <summary>
    /// The retry delay in seconds for the next attempt.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// When the next retry attempt should be made.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// The processing result data in JSON format.
    /// </summary>
    public string? ProcessingResult { get; set; }

    /// <summary>
    /// The version of the message schema for compatibility checking.
    /// </summary>
    public string? MessageSchemaVersion { get; set; }

    /// <summary>
    /// The priority of the message for processing order.
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
}

/// <summary>
/// Enumeration of message processing statuses.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>
    /// Message is pending processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Message was processed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Message processing failed and is awaiting retry.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Message exceeded maximum retry attempts.
    /// </summary>
    DeadLettered = 4,

    /// <summary>
    /// Message processing was cancelled.
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// Enumeration of message priorities.
/// </summary>
public enum MessagePriority
{
    /// <summary>
    /// Low priority message.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority message.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority message.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority message.
    /// </summary>
    Critical = 3
}
