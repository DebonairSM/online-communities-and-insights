using FluentAssertions;
using OnlineCommunities.Api.Extensions;
using System.Security.Claims;

namespace OnlineCommunities.Core.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsGuid_WhenSubClaimIsValidGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenSubClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "test@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenSubClaimIsNotValidGuid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("sub", "not-a-guid")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEmail_ReturnsEmail_FromEmailClaim()
    {
        // Arrange
        var email = "user@example.com";
        var claims = new List<Claim>
        {
            new Claim("email", email)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetEmail();

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void GetEmail_ReturnsEmailFromClaimTypes_WhenStandardClaimUsed()
    {
        // Arrange
        var email = "fallback@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetEmail();

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void GetTenantId_ReturnsGuid_WhenExtensionTenantIdClaimExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("extension_TenantId", tenantId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetTenantId_ReturnsNull_WhenClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "test@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRoles_ReturnsRoles_FromExtensionRolesClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("extension_Roles", "Admin"),
            new Claim("extension_Roles", "Moderator")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetRoles();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Admin");
        result.Should().Contain("Moderator");
    }

    [Fact]
    public void GetRoles_ReturnsEmpty_WhenNoRoleClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "test@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetRoles();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetEntraIdSubject_ReturnsSubject_FromSubClaim()
    {
        // Arrange
        var subject = "entra-subject-12345";
        var claims = new List<Claim>
        {
            new Claim("sub", subject)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetEntraIdSubject();

        // Assert
        result.Should().Be(subject);
    }

    [Fact]
    public void GetEntraOid_ReturnsOid_FromOidClaim()
    {
        // Arrange
        var oid = "entra-oid-67890";
        var claims = new List<Claim>
        {
            new Claim("oid", oid)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetEntraOid();

        // Assert
        result.Should().Be(oid);
    }

    [Fact]
    public void GetDisplayName_ReturnsName_FromNameClaim()
    {
        // Arrange
        var displayName = "John Doe";
        var claims = new List<Claim>
        {
            new Claim("name", displayName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = principal.GetDisplayName();

        // Assert
        result.Should().Be(displayName);
    }
}

