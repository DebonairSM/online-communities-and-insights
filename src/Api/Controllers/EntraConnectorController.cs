using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Core.Interfaces;
using System.Text.Json;

namespace OnlineCommunities.Api.Controllers;

/// <summary>
/// API Connector endpoint for Microsoft Entra External ID token enrichment.
/// Called by Entra after user authenticates to add custom claims for tenant context and roles.
/// </summary>
[ApiController]
[Route("api/entra-connector")]
public class EntraConnectorController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantMembershipRepository _tenantMembershipRepository;
    private readonly ILogger<EntraConnectorController> _logger;

    public EntraConnectorController(
        IUserRepository userRepository,
        ITenantMembershipRepository tenantMembershipRepository,
        ILogger<EntraConnectorController> logger)
    {
        _userRepository = userRepository;
        _tenantMembershipRepository = tenantMembershipRepository;
        _logger = logger;
    }

    /// <summary>
    /// Token enrichment endpoint called by Entra External ID API Connector.
    /// Adds custom claims (tenant ID and roles) to the JWT token.
    /// </summary>
    /// <param name="request">Entra token request containing user information</param>
    /// <returns>Custom attributes to include in the token</returns>
    [HttpPost("token-enrichment")]
    [AllowAnonymous] // Entra validates the request with basic auth or certificate
    public async Task<IActionResult> EnrichToken([FromBody] EntraTokenRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Token enrichment request for user {Email} from provider {IdentityProvider}",
                request.Email, request.IdentityProvider);

            if (string.IsNullOrEmpty(request.Email))
            {
                _logger.LogWarning("Token enrichment failed: No email provided");
                return BadRequest(new { error = "Email is required" });
            }

            // Look up user by email first, then by Entra OID
            var user = await _userRepository.GetByEmailAsync(request.Email);

            // Also check by Entra OID in case email changed
            if (user == null && !string.IsNullOrEmpty(request.ObjectId))
            {
                user = await _userRepository.GetByEntraOidAsync(request.ObjectId);
            }

            if (user == null)
            {
                // JIT provision user from Entra token request
                user = await CreateUserFromEntra(request);
                _logger.LogInformation("JIT provisioned user {UserId} for email {Email}", user.Id, request.Email);
            }
            else
            {
                // Update user with Entra OID if not already set
                if (string.IsNullOrEmpty(user.EntraIdSubject) && !string.IsNullOrEmpty(request.ObjectId))
                {
                    user.EntraIdSubject = request.ObjectId;
                    user.AuthMethod = AuthenticationMethod.EntraExternalId;
                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation("Updated existing user {UserId} with Entra OID", user.Id);
                }
            }

            // Get primary tenant membership
            TenantMembership? membership = null;
            try
            {
                membership = await _tenantMembershipRepository.GetPrimaryForUserAsync(user.Id);
            }
            catch (NotImplementedException)
            {
                // Fallback: Get first membership if GetPrimaryForUserAsync not implemented yet
                var memberships = await _tenantMembershipRepository.GetByUserIdAsync(user.Id);
                membership = memberships.FirstOrDefault();
            }

            // Prepare response for Entra
            var response = new
            {
                // Custom attributes will be prefixed with 'extension_' by Entra
                TenantId = membership?.TenantId.ToString(),
                Roles = membership != null ? new[] { membership.RoleName } : new[] { "Member" }
            };

            _logger.LogInformation(
                "Token enrichment complete for user {UserId}, tenant {TenantId}, role {Role}",
                user.Id, membership?.TenantId, membership?.RoleName ?? "Member");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in token enrichment for email {Email}", request.Email);
            return StatusCode(500, new { error = "Token enrichment failed" });
        }
    }

    private async Task<User> CreateUserFromEntra(EntraTokenRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email ?? throw new ArgumentException("Email is required"),
            FirstName = ExtractFirstName(request.Name) ?? string.Empty,
            LastName = ExtractLastName(request.Name) ?? string.Empty,
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true, // Trust Entra External ID
            IsActive = true,
            // Store Entra object ID for future reference
            EntraIdSubject = request.ObjectId
        };

        await _userRepository.AddAsync(user);
        return user;
    }

    private static string? ExtractFirstName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return null;

        var nameParts = fullName.Split(' ', 2);
        return nameParts.Length > 0 ? nameParts[0] : null;
    }

    private static string? ExtractLastName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return null;

        var nameParts = fullName.Split(' ', 2);
        return nameParts.Length > 1 ? nameParts[1] : null;
    }
}

/// <summary>
/// Request model for Entra External ID token enrichment API Connector.
/// </summary>
public class EntraTokenRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("objectId")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("identityProvider")]
    public string? IdentityProvider { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }
}
