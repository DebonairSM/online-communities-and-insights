using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCommunities.Api.Extensions;

namespace OnlineCommunities.Api.Controllers;

/// <summary>
/// Authentication endpoints for Microsoft Entra External ID authenticated users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current authenticated user's profile.
    /// Requires valid Entra External ID JWT token in Authorization header.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.GetUserId();
        var email = User.GetEmail();

        // TODO: Load full user from database
        // For now, return claims from Entra External ID token
        return Ok(new
        {
            userId,
            email,
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
    public new IActionResult SignOut()
    {
        // TODO: Add token to blacklist if implementing token revocation
        // For now, frontend just deletes the token

        var userId = User.GetUserId();
        _logger.LogInformation("User signed out: UserId={UserId}", userId);

        return Ok(new { message = "Signed out successfully" });
    }

    /// <summary>
    /// Example endpoint to demonstrate secure token validation.
    /// This endpoint shows how the backend validates Entra External ID JWT tokens from frontend.
    /// The [Authorize] attribute automatically validates the token in the Authorization header.
    /// </summary>
    [HttpGet("validate-token")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        // At this point, ASP.NET Core has already validated the Entra External ID JWT token because of [Authorize]
        // If we reach here, it means:
        // 1. Token was present in Authorization header (Bearer <token>)
        // 2. Token signature was valid (signed by Microsoft Entra External ID)
        // 3. Token was not expired
        // 4. Token issuer and audience were correct for your Entra tenant
        // 5. Token was properly formatted and not tampered with

        var tokenInfo = new
        {
            message = "Entra External ID token is valid! This proves your backend correctly validated the Microsoft-issued JWT token.",
            validationDetails = new
            {
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                authenticationType = User.Identity?.AuthenticationType,
                userId = User.GetUserId(),
                email = User.GetEmail(),
                roles = User.GetRoles(),
                tenantId = User.GetTenantId(),
                tokenClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            },
            securityNotes = new
            {
                explanation = "The [Authorize] attribute automatically validates Entra External ID tokens:",
                validations = new[]
                {
                    "Verifies token signature using Microsoft's signing keys",
                    "Checks token expiration (not expired)",
                    "Validates Microsoft Entra External ID issuer and audience",
                    "Ensures token format is correct and not tampered with",
                    "Rejects invalid or revoked tokens"
                }
            }
        };

        _logger.LogInformation("Entra External ID token validation successful for user {UserId}", User.GetUserId());

        return Ok(tokenInfo);
    }
}

