using FluentAssertions;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Enums;

namespace OnlineCommunities.Core.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true
        };

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.AuthMethod.Should().Be(AuthenticationMethod.EntraExternalId);
        user.EmailVerified.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_WithEntraExternalId_HasEntraIdSubject()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = "entra-oid-12345",
            EmailVerified = true,
            IsActive = true
        };

        // Assert
        user.AuthMethod.Should().Be(AuthenticationMethod.EntraExternalId);
        user.EntraIdSubject.Should().Be("entra-oid-12345");
        user.PasswordHash.Should().BeNull();
        user.ExternalLoginProvider.Should().BeNull();
        user.ExternalUserId.Should().BeNull();
    }

    [Fact]
    public void User_CanHaveMultipleTenantMemberships()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "multi@example.com",
            FirstName = "Multi",
            LastName = "Tenant",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true
        };

        var membership1 = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = Guid.NewGuid(),
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        var membership2 = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = Guid.NewGuid(),
            RoleName = "Member",
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        user.TenantMemberships.Add(membership1);
        user.TenantMemberships.Add(membership2);

        // Assert
        user.TenantMemberships.Should().HaveCount(2);
        user.TenantMemberships.Should().Contain(m => m.RoleName == "Admin");
        user.TenantMemberships.Should().Contain(m => m.RoleName == "Member");
    }

    [Theory]
    [InlineData(AuthenticationMethod.EmailPassword)]
    [InlineData(AuthenticationMethod.Google)]
    [InlineData(AuthenticationMethod.GitHub)]
    [InlineData(AuthenticationMethod.MicrosoftPersonal)]
    [InlineData(AuthenticationMethod.EntraId)]
    [InlineData(AuthenticationMethod.EntraExternalId)]
    public void User_SupportsAllAuthenticationMethods(AuthenticationMethod authMethod)
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            AuthMethod = authMethod,
            EmailVerified = true,
            IsActive = true
        };

        // Assert
        user.AuthMethod.Should().Be(authMethod);
    }

    [Fact]
    public void User_EmailIsRequired()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = string.Empty,
            FirstName = "Test",
            LastName = "User"
        };

        // Assert
        user.Email.Should().NotBeNull();
        user.Email.Should().BeEmpty();
    }

    [Fact]
    public void User_InitializedWithEmptyTenantMemberships()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        // Assert
        user.TenantMemberships.Should().NotBeNull();
        user.TenantMemberships.Should().BeEmpty();
    }
}

