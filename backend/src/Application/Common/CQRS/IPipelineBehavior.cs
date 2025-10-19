namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Pipeline behavior for cross-cutting concerns (validation, logging, error handling, etc.)
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken);
}

/// <summary>
/// Delegate representing the next handler in the pipeline
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

