using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Infrastructure.Mediator;

/// <summary>
/// Default implementation of the mediator pattern for dispatching commands and queries
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType) 
            ?? throw new InvalidOperationException($"No handler registered for command {commandType.Name}");

        // Get pipeline behaviors
        var behaviorsType = typeof(IEnumerable<>).MakeGenericType(
            typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(TResponse)));
        
        var behaviors = (_serviceProvider.GetService(behaviorsType) as IEnumerable<object>)?.Reverse().ToList() 
            ?? new List<object>();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = async () =>
        {
            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync));
            var result = handleMethod!.Invoke(handler, new object[] { command, cancellationToken });
            return await (Task<TResponse>)result!;
        };

        // Wrap with behaviors
        foreach (var behavior in behaviors)
        {
            var currentDelegate = handlerDelegate;
            var behaviorType = behavior.GetType();
            
            handlerDelegate = async () =>
            {
                var handleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<object, object>.HandleAsync));
                var result = handleMethod!.Invoke(behavior, new object[] { command, currentDelegate, cancellationToken });
                return await (Task<TResponse>)result!;
            };
        }

        return await handlerDelegate();
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        
        var handler = _serviceProvider.GetService(handlerType) 
            ?? throw new InvalidOperationException($"No handler registered for command {commandType.Name}");

        // Get pipeline behaviors
        var behaviorsType = typeof(IEnumerable<>).MakeGenericType(
            typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(Unit)));
        
        var behaviors = (_serviceProvider.GetService(behaviorsType) as IEnumerable<object>)?.Reverse().ToList() 
            ?? new List<object>();

        // Build the pipeline
        RequestHandlerDelegate<Unit> handlerDelegate = async () =>
        {
            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync));
            var result = handleMethod!.Invoke(handler, new object[] { command, cancellationToken });
            await (Task)result!;
            return Unit.Value;
        };

        // Wrap with behaviors
        foreach (var behavior in behaviors)
        {
            var currentDelegate = handlerDelegate;
            var behaviorType = behavior.GetType();
            
            handlerDelegate = async () =>
            {
                var handleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<object, object>.HandleAsync));
                var result = handleMethod!.Invoke(behavior, new object[] { command, currentDelegate, cancellationToken });
                return await (Task<Unit>)result!;
            };
        }

        await handlerDelegate();
    }

    public async Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType) 
            ?? throw new InvalidOperationException($"No handler registered for query {queryType.Name}");

        // Get pipeline behaviors
        var behaviorsType = typeof(IEnumerable<>).MakeGenericType(
            typeof(IPipelineBehavior<,>).MakeGenericType(queryType, typeof(TResponse)));
        
        var behaviors = (_serviceProvider.GetService(behaviorsType) as IEnumerable<object>)?.Reverse().ToList() 
            ?? new List<object>();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = async () =>
        {
            var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.HandleAsync));
            var result = handleMethod!.Invoke(handler, new object[] { query, cancellationToken });
            return await (Task<TResponse>)result!;
        };

        // Wrap with behaviors
        foreach (var behavior in behaviors)
        {
            var currentDelegate = handlerDelegate;
            var behaviorType = behavior.GetType();
            
            handlerDelegate = async () =>
            {
                var handleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<object, object>.HandleAsync));
                var result = handleMethod!.Invoke(behavior, new object[] { query, currentDelegate, cancellationToken });
                return await (Task<TResponse>)result!;
            };
        }

        return await handlerDelegate();
    }
}

