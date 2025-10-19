using Microsoft.Extensions.Logging;
using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs command and query execution
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            
            _logger.LogInformation("Handled {RequestName} successfully", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}

