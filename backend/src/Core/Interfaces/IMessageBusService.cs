using OnlineCommunities.Core.Entities.Notifications;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Service for publishing and consuming messages through Azure Service Bus.
/// Provides abstraction over Service Bus operations for loose coupling.
/// </summary>
public interface IMessageBusService
{
    /// <summary>
    /// Publishes a message to a Service Bus topic.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="topicName">The name of the topic to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message ID assigned by Service Bus.</returns>
    Task<string> PublishAsync<T>(
        string topicName, 
        T message, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publishes a notification event to the notifications topic.
    /// </summary>
    /// <param name="notificationEvent">The notification event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message ID assigned by Service Bus.</returns>
    Task<string> PublishNotificationAsync(
        NotificationEvent notificationEvent, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to a Service Bus queue.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="queueName">The name of the queue to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="correlationId">Optional correlation ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message ID assigned by Service Bus.</returns>
    Task<string> SendToQueueAsync<T>(
        string queueName, 
        T message, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sends a message to a dead letter queue for failed processing.
    /// </summary>
    /// <typeparam name="T">The type of message to send to DLQ.</typeparam>
    /// <param name="originalTopicName">The original topic name.</param>
    /// <param name="message">The message to send to DLQ.</param>
    /// <param name="reason">The reason for sending to DLQ.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message ID assigned by Service Bus.</returns>
    Task<string> SendToDeadLetterQueueAsync<T>(
        string originalTopicName, 
        T message, 
        string reason, 
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if a topic exists in Service Bus.
    /// </summary>
    /// <param name="topicName">The name of the topic to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the topic exists, false otherwise.</returns>
    Task<bool> TopicExistsAsync(string topicName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a queue exists in Service Bus.
    /// </summary>
    /// <param name="queueName">The name of the queue to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the queue exists, false otherwise.</returns>
    Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection status of the Service Bus.
    /// </summary>
    /// <returns>True if connected, false otherwise.</returns>
    bool IsConnected { get; }
}
