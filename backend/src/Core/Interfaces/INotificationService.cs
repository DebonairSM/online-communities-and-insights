using OnlineCommunities.Core.Entities.Notifications;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Service for orchestrating notification delivery through various channels.
/// Handles template processing, recipient validation, and delivery coordination.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification using the specified template and recipient data.
    /// </summary>
    /// <param name="templateName">The name of the template to use.</param>
    /// <param name="recipient">The recipient's contact information.</param>
    /// <param name="templateData">The data to substitute in the template.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification event ID.</returns>
    Task<Guid> SendNotificationAsync(
        string templateName,
        string recipient,
        Dictionary<string, object> templateData,
        Guid tenantId,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification using a custom template without a stored template.
    /// </summary>
    /// <param name="subject">The subject line.</param>
    /// <param name="content">The message content.</param>
    /// <param name="recipient">The recipient's contact information.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="notificationType">The type of notification (email, sms, etc.).</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification event ID.</returns>
    Task<Guid> SendCustomNotificationAsync(
        string subject,
        string content,
        string recipient,
        Guid tenantId,
        string notificationType = "email",
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a notification to be sent at a specific time.
    /// </summary>
    /// <param name="templateName">The name of the template to use.</param>
    /// <param name="recipient">The recipient's contact information.</param>
    /// <param name="templateData">The data to substitute in the template.</param>
    /// <param name="tenantId">The tenant ID for multi-tenant support.</param>
    /// <param name="scheduledAt">When to send the notification.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification event ID.</returns>
    Task<Guid> ScheduleNotificationAsync(
        string templateName,
        string recipient,
        Dictionary<string, object> templateData,
        Guid tenantId,
        DateTime scheduledAt,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled notification.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancelled successfully, false if not found or already sent.</returns>
    Task<bool> CancelNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a notification.
    /// </summary>
    /// <param name="notificationId">The ID of the notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification event or null if not found.</returns>
    Task<NotificationEvent?> GetNotificationStatusAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed notification.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if retry was initiated, false if not found or max retries exceeded.</returns>
    Task<bool> RetryNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific tenant with optional filtering.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="notificationType">Optional type filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of notification events.</returns>
    Task<IEnumerable<NotificationEvent>> GetNotificationsAsync(
        Guid tenantId,
        NotificationStatus? status = null,
        string? notificationType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
