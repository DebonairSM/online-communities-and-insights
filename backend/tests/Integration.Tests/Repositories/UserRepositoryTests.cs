using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Entities.Tenants;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Infrastructure.Data;
using OnlineCommunities.Infrastructure.Repositories;

namespace OnlineCommunities.Integration.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsUserToDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = "entra-oid-123",
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(user);

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
        savedUser.FirstName.Should().Be("Test");
        savedUser.LastName.Should().Be("User");
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenEmailExists()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "findme@example.com",
            FirstName = "Find",
            LastName = "Me",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("findme@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("findme@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenEmailDoesNotExist()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEntraIdSubjectAsync_ReturnsUser_WhenEntraOidExists()
    {
        // Arrange
        var entraOid = "entra-oid-99999";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "entra@example.com",
            FirstName = "Entra",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EntraIdSubject = entraOid,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEntraIdSubjectAsync(entraOid);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.EntraIdSubject.Should().Be(entraOid);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTenantMemberships()
    {
        // Arrange
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
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
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

        _context.Tenants.Add(tenant);
        _context.Users.Add(user);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantMemberships.Should().HaveCount(1);
        result.TenantMemberships.First().RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserInDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "update@example.com",
            FirstName = "Old",
            LastName = "Name",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        user.FirstName = "New";
        user.LastName = "Updated";
        await _repository.UpdateAsync(user);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("New");
        updatedUser.LastName.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserFromDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "delete@example.com",
            FirstName = "Delete",
            LastName = "Me",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(user.Id);

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task IsMemberOfTenantAsync_ReturnsTrue_WhenUserIsMember()
    {
        // Arrange
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
            Email = "member@example.com",
            FirstName = "Member",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            RoleName = "Member",
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        _context.Users.Add(user);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsMemberOfTenantAsync(userId, tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsMemberOfTenantAsync_ReturnsFalse_WhenUserIsNotMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _repository.IsMemberOfTenantAsync(userId, tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserTenantIdsAsync_ReturnsAllTenantIds_ForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "multitenent@example.com",
            FirstName = "Multi",
            LastName = "Tenant",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var tenant1 = new Tenant
        {
            Id = tenant1Id,
            Name = "Tenant One",
            Subdomain = "tenant1",
            IsActive = true,
            SubscriptionTier = "Free",
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            Id = tenant2Id,
            Name = "Tenant Two",
            Subdomain = "tenant2",
            IsActive = true,
            SubscriptionTier = "Free",
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };

        var membership1 = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenant1Id,
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership2 = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenant2Id,
            RoleName = "Member",
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Tenants.AddRange(tenant1, tenant2);
        _context.TenantMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserTenantIdsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(tenant1Id);
        result.Should().Contain(tenant2Id);
    }
}

