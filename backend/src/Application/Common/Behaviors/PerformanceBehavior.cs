using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs performance warnings for slow requests
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        _timer.Start();
        
        var response = await next();
        
        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName, 
                elapsedMilliseconds);
        }

        return response;
    }
}

