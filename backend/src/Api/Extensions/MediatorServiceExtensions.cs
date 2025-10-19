using System.Reflection;
using FluentValidation;
using OnlineCommunities.Application.Common.Behaviors;
using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Infrastructure.Mediator;

namespace OnlineCommunities.Api.Extensions;

/// <summary>
/// Extension methods for registering mediator and CQRS services
/// </summary>
public static class MediatorServiceExtensions
{
    /// <summary>
    /// Registers the mediator and all command/query handlers from the Application assembly
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        // Register the mediator
        services.AddScoped<IMediator, Infrastructure.Mediator.Mediator>();

        var applicationAssembly = Assembly.Load("OnlineCommunities.Application");

        // Register all command handlers
        RegisterHandlers(services, applicationAssembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, applicationAssembly, typeof(ICommandHandler<>));

        // Register all query handlers
        RegisterHandlers(services, applicationAssembly, typeof(IQueryHandler<,>));

        // Register all validators
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Register pipeline behaviors (order matters - they execute in registration order)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }

    private static void RegisterHandlers(
        IServiceCollection services, 
        Assembly assembly, 
        Type handlerInterfaceType)
    {
        var handlers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
            .SelectMany(t => t.GetInterfaces(), (implementation, @interface) => new { implementation, @interface })
            .Where(x => x.@interface.IsGenericType 
                && x.@interface.GetGenericTypeDefinition() == handlerInterfaceType)
            .ToList();

        foreach (var handler in handlers)
        {
            services.AddScoped(handler.@interface, handler.implementation);
        }
    }
}

