namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Handler for queries that retrieve data
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

