namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Mediator for dispatching commands and queries to their handlers
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a command that returns a response
    /// </summary>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command without a return value
    /// </summary>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a query to retrieve data
    /// </summary>
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}

