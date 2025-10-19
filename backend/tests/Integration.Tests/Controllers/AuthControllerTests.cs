using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace OnlineCommunities.Integration.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAuthStatus_ReturnsUnauthenticated_WithoutToken()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("authenticated").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ValidateToken_Returns401_WithoutToken()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_Returns401_WithoutToken()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignOut_Returns401_WithoutToken()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/signout");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

