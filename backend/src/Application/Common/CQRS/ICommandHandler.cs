namespace OnlineCommunities.Application.Common.CQRS;

/// <summary>
/// Handler for commands that modify state
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for commands that modify state without returning a value
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

