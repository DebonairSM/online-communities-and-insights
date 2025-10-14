using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OnlineCommunities.Application.Services.Identity;

/// <summary>
/// Service for handling external OAuth authentication (social login).
/// Implements JIT (Just-In-Time) provisioning: creates users on first login.
/// </summary>
public class ExternalAuthService : IExternalAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<ExternalAuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(User user, string jwtToken)> HandleExternalLoginAsync(
        string provider,
        string externalUserId,
        string email,
        string? firstName,
        string? lastName)
    {
        // Normalize provider name
        provider = NormalizeProviderName(provider);

        // Look for existing user by external login
        var user = await _userRepository.GetByExternalLoginAsync(provider, externalUserId);

        if (user == null)
        {
            // JIT Provisioning - create new user
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                ExternalLoginProvider = provider,
                ExternalUserId = externalUserId,
                AuthMethod = MapProviderToAuthMethod(provider),
                EmailVerified = true,  // Trust OAuth provider
                IsActive = true,
                PasswordHash = null,  // No password for social login
                EntraTenantId = null,  // Not enterprise SSO
                EntraIdSubject = null  // Not enterprise SSO
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation(
                "JIT provisioned new user via {Provider}: UserId={UserId}, Email={Email}",
                provider, user.Id, email);

            // TODO: Publish UserCreatedEvent for event-driven workflows
            // TODO: Send welcome email
            // TODO: Assign to default tenant if needed
        }
        else
        {
            // User exists - optionally update profile
            var profileUpdated = false;

            if (!string.IsNullOrEmpty(email) && user.Email != email)
            {
                user.Email = email;
                profileUpdated = true;
            }

            if (!string.IsNullOrEmpty(firstName) && user.FirstName != firstName)
            {
                user.FirstName = firstName;
                profileUpdated = true;
            }

            if (!string.IsNullOrEmpty(lastName) && user.LastName != lastName)
            {
                user.LastName = lastName;
                profileUpdated = true;
            }

            if (profileUpdated)
            {
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation(
                    "Updated user profile from OAuth: UserId={UserId}",
                    user.Id);
            }
            else
            {
                _logger.LogInformation(
                    "User logged in via {Provider}: UserId={UserId}, Email={Email}",
                    provider, user.Id, email);
            }
        }

        // Generate JWT token for API access
        var jwtToken = GenerateJwtToken(user);

        return (user, jwtToken);
    }

    public string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured");
        
        var issuer = _configuration["JwtSettings:Issuer"] ?? "OnlineCommunitiesAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "OnlineCommunitiesUsers";
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("auth_method", user.AuthMethod.ToString()),
            new Claim("user_id", user.Id.ToString())  // For easy access
        };

        // Add name claims if available
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<Guid?> ValidateTokenAsync(string token)
    {
        try
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured");
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst("user_id");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private string NormalizeProviderName(string provider)
    {
        return provider.ToLower() switch
        {
            "google" => "Google",
            "github" => "GitHub",
            "microsoft" => "Microsoft",
            _ => provider
        };
    }

    private AuthenticationMethod MapProviderToAuthMethod(string provider)
    {
        return provider switch
        {
            "Google" => AuthenticationMethod.Google,
            "GitHub" => AuthenticationMethod.GitHub,
            "Microsoft" => AuthenticationMethod.MicrosoftPersonal,
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
    }
}

