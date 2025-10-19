namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Marker interface for queries that retrieve data without side effects
/// </summary>
public interface IQuery<out TResponse>
{
}

