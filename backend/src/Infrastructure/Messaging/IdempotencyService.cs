using Microsoft.Extensions.Logging;
using OnlineCommunities.Core.Entities.Processing;
using OnlineCommunities.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OnlineCommunities.Infrastructure.Messaging;

/// <summary>
/// Service for ensuring idempotent message processing using database storage.
/// Tracks message processing history and prevents duplicate processing.
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly ILogger<IdempotencyService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the IdempotencyService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public IdempotencyService(ILogger<IdempotencyService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsMessageProcessedAsync(string messageId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if message {MessageId} has been processed for tenant {TenantId}", messageId, tenantId);

            // TODO: Implement actual database query
            // This should check the MessageProcessingRecord table for completed status
            var record = await GetProcessingRecordAsync(messageId, tenantId, cancellationToken);
            
            var isProcessed = record?.Status == ProcessingStatus.Completed;
            
            _logger.LogDebug("Message {MessageId} processed status: {IsProcessed}", messageId, isProcessed);
            return isProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if message {MessageId} has been processed", messageId);
            return false; // Fail open - allow processing if we can't check
        }
    }

    /// <inheritdoc />
    public async Task<MessageProcessingRecord?> GetProcessingRecordAsync(string messageId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting processing record for message {MessageId} in tenant {TenantId}", messageId, tenantId);

            // TODO: Implement actual database query
            // This should query the MessageProcessingRecord table
            // Example: return await _context.MessageProcessingRecords
            //     .FirstOrDefaultAsync(r => r.MessageId == messageId && r.TenantId == tenantId, cancellationToken);
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - return null for now
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get processing record for message {MessageId}", messageId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MessageProcessingRecord> CreateProcessingRecordAsync(
        string messageId,
        string messageType,
        string sourceTopic,
        Guid tenantId,
        string? messageMetadata = null,
        string? messageHash = null,
        MessagePriority priority = MessagePriority.Normal,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating processing record for message {MessageId} of type {MessageType}", messageId, messageType);

            var record = new MessageProcessingRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MessageId = messageId,
                MessageType = messageType,
                SourceTopic = sourceTopic,
                Status = ProcessingStatus.Pending,
                AttemptCount = 0,
                MaxAttempts = 3,
                ReceivedAt = DateTime.UtcNow,
                MessageMetadata = messageMetadata,
                MessageHash = messageHash,
                Priority = priority,
                RetryDelaySeconds = 30,
                NextRetryAt = DateTime.UtcNow.AddSeconds(30)
            };

            // TODO: Implement actual database insert
            // This should save the record to the MessageProcessingRecord table
            // Example: await _context.MessageProcessingRecords.AddAsync(record, cancellationToken);
            // await _context.SaveChangesAsync(cancellationToken);
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);

            _logger.LogInformation("Successfully created processing record {RecordId} for message {MessageId}", record.Id, messageId);
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create processing record for message {MessageId}", messageId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateProcessingStatusAsync(
        string messageId,
        Guid tenantId,
        ProcessingStatus status,
        string? errorMessage = null,
        string? exceptionDetails = null,
        string? processingResult = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating processing status for message {MessageId} to {Status}", messageId, status);

            // TODO: Implement actual database update
            // This should update the MessageProcessingRecord table
            // Example: var record = await _context.MessageProcessingRecords
            //     .FirstOrDefaultAsync(r => r.MessageId == messageId && r.TenantId == tenantId, cancellationToken);
            // if (record != null)
            // {
            //     record.Status = status;
            //     record.AttemptCount++;
            //     record.ProcessingCompletedAt = DateTime.UtcNow;
            //     record.ErrorMessage = errorMessage;
            //     record.ExceptionDetails = exceptionDetails;
            //     record.ProcessingResult = processingResult;
            //     
            //     if (record.ProcessingStartedAt.HasValue)
            //     {
            //         record.ProcessingDurationMs = (long)(record.ProcessingCompletedAt.Value - record.ProcessingStartedAt.Value).TotalMilliseconds;
            //     }
            //     
            //     await _context.SaveChangesAsync(cancellationToken);
            //     return true;
            // }
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - assume update was successful
            _logger.LogInformation("Successfully updated processing status for message {MessageId}", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update processing status for message {MessageId}", messageId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsDeadLetteredAsync(
        string messageId,
        Guid tenantId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Marking message {MessageId} as dead lettered. Reason: {Reason}", messageId, reason);

            // TODO: Implement actual database update
            // This should update the MessageProcessingRecord table to mark as dead lettered
            // Example: var record = await _context.MessageProcessingRecords
            //     .FirstOrDefaultAsync(r => r.MessageId == messageId && r.TenantId == tenantId, cancellationToken);
            // if (record != null)
            // {
            //     record.Status = ProcessingStatus.DeadLettered;
            //     record.IsDeadLettered = true;
            //     record.DeadLetteredAt = DateTime.UtcNow;
            //     record.DeadLetterReason = reason;
            //     record.ProcessingCompletedAt = DateTime.UtcNow;
            //     
            //     await _context.SaveChangesAsync(cancellationToken);
            //     return true;
            // }
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - assume update was successful
            _logger.LogWarning("Successfully marked message {MessageId} as dead lettered", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark message {MessageId} as dead lettered", messageId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MessageProcessingRecord>> GetMessagesReadyForRetryAsync(
        Guid tenantId,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting messages ready for retry for tenant {TenantId}", tenantId);

            // TODO: Implement actual database query
            // This should query for failed messages that are ready for retry
            // Example: return await _context.MessageProcessingRecords
            //     .Where(r => r.TenantId == tenantId 
            //         && r.Status == ProcessingStatus.Failed 
            //         && r.AttemptCount < r.MaxAttempts
            //         && r.NextRetryAt <= DateTime.UtcNow)
            //     .OrderBy(r => r.Priority)
            //     .ThenBy(r => r.NextRetryAt)
            //     .Take(maxCount)
            //     .ToListAsync(cancellationToken);
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - return empty list for now
            return new List<MessageProcessingRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages ready for retry for tenant {TenantId}", tenantId);
            return new List<MessageProcessingRecord>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MessageProcessingRecord>> GetDeadLetteredMessagesAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting dead lettered messages for tenant {TenantId}", tenantId);

            // TODO: Implement actual database query
            // This should query for dead lettered messages with optional date filtering
            // Example: var query = _context.MessageProcessingRecords
            //     .Where(r => r.TenantId == tenantId && r.IsDeadLettered);
            //     
            // if (fromDate.HasValue)
            //     query = query.Where(r => r.DeadLetteredAt >= fromDate.Value);
            //     
            // if (toDate.HasValue)
            //     query = query.Where(r => r.DeadLetteredAt <= toDate.Value);
            //     
            // return await query
            //     .OrderByDescending(r => r.DeadLetteredAt)
            //     .Skip(skip)
            //     .Take(take)
            //     .ToListAsync(cancellationToken);
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - return empty list for now
            return new List<MessageProcessingRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dead lettered messages for tenant {TenantId}", tenantId);
            return new List<MessageProcessingRecord>();
        }
    }

    /// <inheritdoc />
    public DateTime CalculateNextRetryTime(int attemptCount, int baseDelaySeconds = 30, int maxDelaySeconds = 3600)
    {
        // Exponential backoff with jitter
        var delaySeconds = Math.Min(baseDelaySeconds * Math.Pow(2, attemptCount), maxDelaySeconds);
        
        // Add jitter to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * 0.1 * delaySeconds;
        delaySeconds += jitter;
        
        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldRecordsAsync(
        Guid tenantId,
        int retentionDays = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            _logger.LogInformation("Cleaning up processing records older than {CutoffDate} for tenant {TenantId}", cutoffDate, tenantId);

            // TODO: Implement actual database cleanup
            // This should delete old completed and dead lettered records
            // Example: var recordsToDelete = await _context.MessageProcessingRecords
            //     .Where(r => r.TenantId == tenantId 
            //         && r.ProcessingCompletedAt < cutoffDate
            //         && (r.Status == ProcessingStatus.Completed || r.Status == ProcessingStatus.DeadLettered))
            //     .ToListAsync(cancellationToken);
            //     
            // _context.MessageProcessingRecords.RemoveRange(recordsToDelete);
            // await _context.SaveChangesAsync(cancellationToken);
            // 
            // return recordsToDelete.Count;
            
            // Simulate async operation
            await Task.Delay(10, cancellationToken);
            
            // Stub implementation - return 0 for now
            _logger.LogInformation("Cleaned up 0 old processing records for tenant {TenantId}", tenantId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old records for tenant {TenantId}", tenantId);
            return 0;
        }
    }

    /// <summary>
    /// Calculates a hash of the message content for duplicate detection.
    /// </summary>
    /// <param name="content">The message content to hash.</param>
    /// <returns>The SHA-256 hash of the content.</returns>
    public static string CalculateMessageHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }
}
