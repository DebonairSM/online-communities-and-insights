using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Api.Extensions;
using System.Security.Claims;

namespace OnlineCommunities.Api.Controllers;

/// <summary>
/// Authentication endpoints for social login (OAuth 2.0).
/// Supports Google, GitHub, and Microsoft Personal Accounts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IExternalAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IExternalAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates OAuth login flow for specified provider.
    /// Redirects to provider's login page.
    /// </summary>
    /// <param name="provider">google, github, or microsoft</param>
    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    public IActionResult Login(string provider)
    {
        var providerName = provider.ToLower() switch
        {
            "google" => "Google",
            "github" => "GitHub",
            "microsoft" => "Microsoft",
            _ => null
        };

        if (providerName == null)
        {
            return BadRequest(new { error = $"Unsupported provider: {provider}" });
        }

        // Redirect to OAuth provider
        var redirectUrl = Url.Action(nameof(OAuthCallback), new { provider = providerName });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        
        return Challenge(properties, providerName);
    }

    /// <summary>
    /// OAuth callback endpoint - called by provider after user authenticates.
    /// Creates or updates user, then returns JWT token.
    /// </summary>
    [HttpGet("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string provider)
    {
        try
        {
            // Authenticate with the OAuth provider
            var result = await HttpContext.AuthenticateAsync(provider);

            if (!result.Succeeded)
            {
                _logger.LogWarning("OAuth authentication failed for provider {Provider}", provider);
                return Unauthorized(new { error = "Authentication failed" });
            }

            var claims = result.Principal?.Claims.ToList();
            if (claims == null || !claims.Any())
            {
                _logger.LogError("No claims received from OAuth provider {Provider}", provider);
                return Unauthorized(new { error = "No user information received" });
            }

            // Extract user info from claims
            var externalUserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

            // For GitHub, extract from different claim if needed
            if (provider.ToLower() == "github" && string.IsNullOrEmpty(firstName))
            {
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var nameParts = name.Split(' ', 2);
                    firstName = nameParts.Length > 0 ? nameParts[0] : name;
                    lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
                }
            }

            if (string.IsNullOrEmpty(externalUserId) || string.IsNullOrEmpty(email))
            {
                _logger.LogError(
                    "Missing required claims from OAuth provider {Provider}. UserId={UserId}, Email={Email}",
                    provider, externalUserId, email);
                return Unauthorized(new { error = "Incomplete user information" });
            }

            // Handle login (creates user if doesn't exist)
            var (user, jwtToken) = await _authService.HandleExternalLoginAsync(
                provider, externalUserId, email, firstName, lastName);

            _logger.LogInformation(
                "Successful OAuth login: Provider={Provider}, UserId={UserId}, Email={Email}",
                provider, user.Id, email);

            // TODO: Redirect to frontend with token
            // For now, return JSON response
            // In production, you'd redirect to: https://yourfrontend.com/auth/callback?token={jwtToken}
            return Ok(new
            {
                token = jwtToken,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    authMethod = user.AuthMethod.ToString(),
                    tenantCount = user.TenantMemberships?.Count ?? 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback for provider {Provider}", provider);
            return StatusCode(500, new { error = "An error occurred during authentication" });
        }
    }

    /// <summary>
    /// Get current authenticated user's profile.
    /// Requires valid JWT token in Authorization header.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.GetUserId();
        var email = User.GetEmail();
        var authMethod = User.FindFirst("auth_method")?.Value;

        // TODO: Load full user from database
        // For now, return claims
        return Ok(new
        {
            userId,
            email,
            authMethod,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    /// <summary>
    /// Check if user is authenticated.
    /// Public endpoint for frontend to check auth status.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetAuthStatus()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new
            {
                authenticated = true,
                userId = User.GetUserId(),
                email = User.GetEmail()
            });
        }

        return Ok(new { authenticated = false });
    }

    /// <summary>
    /// Sign out (invalidate token).
    /// Frontend should delete token from storage.
    /// </summary>
    [HttpPost("signout")]
    [Authorize]
    public IActionResult SignOut()
    {
        // TODO: Add token to blacklist if implementing token revocation
        // For now, frontend just deletes the token

        var userId = User.GetUserId();
        _logger.LogInformation("User signed out: UserId={UserId}", userId);

        return Ok(new { message = "Signed out successfully" });
    }
}

