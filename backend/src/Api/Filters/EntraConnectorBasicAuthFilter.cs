using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OnlineCommunities.Api.Configuration;

namespace OnlineCommunities.Api.Filters;

/// <summary>
/// Validates Basic Authentication for Entra API Connector callbacks.
/// Reads expected credentials from configuration section 'EntraConnector'.
/// </summary>
public sealed class EntraConnectorBasicAuthFilter : IAsyncActionFilter
{
    private readonly IOptionsMonitor<EntraConnectorOptions> _options;
    private readonly ILogger<EntraConnectorBasicAuthFilter> _logger;

    public EntraConnectorBasicAuthFilter(
        IOptionsMonitor<EntraConnectorOptions> options,
        ILogger<EntraConnectorBasicAuthFilter> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expected = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(expected.Username) || string.IsNullOrWhiteSpace(expected.Password))
        {
            _logger.LogWarning("EntraConnector basic auth is not configured. Rejecting request.");
            context.Result = new UnauthorizedResult();
            AddWwwAuthenticateHeader(context);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogInformation("Missing Authorization header for EntraConnector call.");
            context.Result = new UnauthorizedResult();
            AddWwwAuthenticateHeader(context);
            return;
        }

        var header = authHeader.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Unsupported auth scheme for EntraConnector call.");
            context.Result = new UnauthorizedResult();
            AddWwwAuthenticateHeader(context);
            return;
        }

        try
        {
            var b64 = header.Substring("Basic ".Length).Trim();
            var bytes = Convert.FromBase64String(b64);
            var decoded = Encoding.UTF8.GetString(bytes);
            var colonIndex = decoded.IndexOf(':');
            if (colonIndex <= 0)
            {
                context.Result = new UnauthorizedResult();
                AddWwwAuthenticateHeader(context);
                return;
            }

            var user = decoded.Substring(0, colonIndex);
            var pass = decoded.Substring(colonIndex + 1);

            if (SecureEquals(user, expected.Username) && SecureEquals(pass, expected.Password))
            {
                await next();
                return;
            }
            else
            {
                _logger.LogInformation("Invalid basic credentials for EntraConnector call.");
                context.Result = new UnauthorizedResult();
                AddWwwAuthenticateHeader(context);
                return;
            }
        }
        catch (FormatException)
        {
            _logger.LogInformation("Invalid base64 Authorization header for EntraConnector call.");
            context.Result = new UnauthorizedResult();
            AddWwwAuthenticateHeader(context);
            return;
        }
    }

    private static void AddWwwAuthenticateHeader(ActionExecutingContext context)
    {
        context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"EntraConnector\"";
    }

    private static bool SecureEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(
            aBytes.Length == bBytes.Length ? aBytes : PadRight(aBytes, Math.Max(aBytes.Length, bBytes.Length)),
            aBytes.Length == bBytes.Length ? bBytes : PadRight(bBytes, Math.Max(aBytes.Length, bBytes.Length))
        );

        static byte[] PadRight(byte[] input, int length)
        {
            if (input.Length == length) return input;
            var padded = new byte[length];
            Buffer.BlockCopy(input, 0, padded, 0, input.Length);
            return padded;
        }
    }
}
