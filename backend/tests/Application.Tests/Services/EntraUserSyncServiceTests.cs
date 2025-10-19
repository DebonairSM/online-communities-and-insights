using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineCommunities.Application.Services.Identity;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Core.Interfaces;
using System.Security.Claims;

namespace OnlineCommunities.Application.Tests.Services;

public class EntraUserSyncServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITenantMembershipRepository> _tenantMembershipRepositoryMock;
    private readonly Mock<ILogger<EntraUserSyncService>> _loggerMock;
    private readonly EntraUserSyncService _service;

    public EntraUserSyncServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tenantMembershipRepositoryMock = new Mock<ITenantMembershipRepository>();
        _loggerMock = new Mock<ILogger<EntraUserSyncService>>();
        _service = new EntraUserSyncService(
            _userRepositoryMock.Object,
            _tenantMembershipRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrCreateUserFromEntraToken_CreatesNewUser_WhenUserDoesNotExist()
    {
        // Arrange
        var entraOid = "entra-oid-12345";
        var email = "newuser@example.com";
        var givenName = "John";
        var familyName = "Doe";

        var claims = new List<Claim>
        {
            new Claim("oid", entraOid),
            new Claim("email", email),
            new Claim("given_name", givenName),
            new Claim("family_name", familyName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _userRepositoryMock.Setup(x => x.GetByEntraOidAsync(entraOid))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _service.GetOrCreateUserFromEntraToken(principal);

        // Assert
        result.Should().NotBeNull();
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(email);
        capturedUser.FirstName.Should().Be(givenName);
        capturedUser.LastName.Should().Be(familyName);
        capturedUser.AuthMethod.Should().Be(AuthenticationMethod.EntraExternalId);
        capturedUser.EntraIdSubject.Should().Be(entraOid);
        capturedUser.EmailVerified.Should().BeTrue();
        capturedUser.IsActive.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserFromEntraToken_ReturnsExistingUser_WhenUserExists()
    {
        // Arrange
        var entraOid = "existing-oid-12345";
        var email = "existing@example.com";
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Jane",
            LastName = "Smith",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = entraOid,
            EmailVerified = true,
            IsActive = true
        };

        var claims = new List<Claim>
        {
            new Claim("oid", entraOid),
            new Claim("email", email),
            new Claim("given_name", "Jane"),
            new Claim("family_name", "Smith")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _userRepositoryMock.Setup(x => x.GetByEntraOidAsync(entraOid))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.GetOrCreateUserFromEntraToken(principal);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingUser.Id);
        result.Email.Should().Be(email);

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateUserFromEntraToken_UpdatesProfile_WhenUserInfoChanged()
    {
        // Arrange
        var entraOid = "existing-oid-12345";
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "old@example.com",
            FirstName = "OldFirst",
            LastName = "OldLast",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = entraOid,
            EmailVerified = true,
            IsActive = true
        };

        var newEmail = "new@example.com";
        var newGivenName = "NewFirst";
        var newFamilyName = "NewLast";

        var claims = new List<Claim>
        {
            new Claim("oid", entraOid),
            new Claim("email", newEmail),
            new Claim("given_name", newGivenName),
            new Claim("family_name", newFamilyName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _userRepositoryMock.Setup(x => x.GetByEntraOidAsync(entraOid))
            .ReturnsAsync(existingUser);

        User? updatedUser = null;
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => updatedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetOrCreateUserFromEntraToken(principal);

        // Assert
        result.Should().NotBeNull();
        updatedUser.Should().NotBeNull();
        updatedUser!.Email.Should().Be(newEmail);
        updatedUser.FirstName.Should().Be(newGivenName);
        updatedUser.LastName.Should().Be(newFamilyName);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserFromEntraToken_ThrowsException_WhenOidClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "test@example.com"),
            new Claim("given_name", "Test"),
            new Claim("family_name", "User")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetOrCreateUserFromEntraToken(principal));
    }

    [Fact]
    public async Task GetOrCreateUserFromEntraToken_ThrowsException_WhenEmailClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("oid", "entra-oid-12345"),
            new Claim("given_name", "Test"),
            new Claim("family_name", "User")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetOrCreateUserFromEntraToken(principal));
    }

    [Fact]
    public async Task GetUserIdByEntraOid_ReturnsUserId_WhenUserExists()
    {
        // Arrange
        var entraOid = "test-oid-12345";
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EntraIdSubject = entraOid
        };

        _userRepositoryMock.Setup(x => x.GetByEntraOidAsync(entraOid))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserIdByEntraOid(entraOid);

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserIdByEntraOid_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        var entraOid = "nonexistent-oid";

        _userRepositoryMock.Setup(x => x.GetByEntraOidAsync(entraOid))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserIdByEntraOid(entraOid);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserIdByEntraOid_ReturnsNull_WhenOidIsEmpty()
    {
        // Act
        var result = await _service.GetUserIdByEntraOid(string.Empty);

        // Assert
        result.Should().BeNull();
    }
}

