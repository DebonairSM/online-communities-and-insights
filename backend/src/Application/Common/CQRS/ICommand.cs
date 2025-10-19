namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Marker interface for commands that modify state and return a response
/// </summary>
public interface ICommand<out TResponse>
{
}

/// <summary>
/// Marker interface for commands that modify state without returning a value
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

