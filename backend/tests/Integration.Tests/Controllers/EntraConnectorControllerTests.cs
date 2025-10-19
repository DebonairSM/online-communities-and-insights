using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Entities.Tenants;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OnlineCommunities.Integration.Tests.Controllers;

public class EntraConnectorControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public EntraConnectorControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task TokenEnrichment_CreatesNewUser_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new
        {
            email = "newuser@example.com",
            objectId = "entra-oid-new-123",
            identityProvider = "google.com",
            name = "New User",
            givenName = "New",
            surname = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/entra-connector/token-enrichment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Should return default Member role for new user
        result.TryGetProperty("Roles", out var roles).Should().BeTrue();
    }

    [Fact]
    public async Task TokenEnrichment_ReturnsBadRequest_WhenEmailMissing()
    {
        // Arrange
        var request = new
        {
            objectId = "entra-oid-123",
            identityProvider = "google.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/entra-connector/token-enrichment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TokenEnrichment_ReturnsRoleAndTenant_ForExistingUserWithMembership()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Subdomain = "test",
            IsActive = true,
            SubscriptionTier = "Free",
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Email = "existing@example.com",
            FirstName = "Existing",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = "entra-oid-existing-456",
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        context.TenantMemberships.Add(membership);
        await context.SaveChangesAsync();

        var request = new
        {
            email = "existing@example.com",
            objectId = "entra-oid-existing-456",
            identityProvider = "microsoft.com",
            name = "Existing User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/entra-connector/token-enrichment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("TenantId").GetString().Should().Be(tenantId.ToString());
        result.GetProperty("Roles").EnumerateArray().First().GetString().Should().Be("Admin");
    }
}

