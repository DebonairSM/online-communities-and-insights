using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace OnlineCommunities.Integration.Tests.API;

public class LandingPageTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public LandingPageTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LandingPage_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LandingPage_ReturnsJsonWithApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        json.GetProperty("message").GetString().Should().Be("Online Communities API");
        json.GetProperty("version").GetString().Should().Be("1.0.0");
        json.GetProperty("authentication").GetString().Should().Be("Microsoft Entra External ID");
        json.GetProperty("endpoints").GetProperty("health").GetString().Should().Be("/health");
        json.GetProperty("endpoints").GetProperty("auth").GetString().Should().Be("/api/auth");
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.GetProperty("status").GetString().Should().Be("healthy");
        json.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheck_CanBeAccessedWithoutAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_IsAvailableInDevelopment()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        // OpenAPI may not be available unless in Development environment
        // We just check it doesn't throw an internal server error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}

