using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Entities.Tenants;
using OnlineCommunities.Core.Enums;
using OnlineCommunities.Infrastructure.Data;
using OnlineCommunities.Infrastructure.Repositories;

namespace OnlineCommunities.Integration.Tests.Repositories;

public class TenantMembershipRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantMembershipRepository _repository;

    public TenantMembershipRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new TenantMembershipRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByUserAndTenantAsync_ReturnsMembership_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var user = CreateTestUser(userId, "test@example.com");
        var tenant = CreateTestTenant(tenantId, "Test Tenant", "test");
        var membership = CreateTestMembership(userId, tenantId, "Admin");

        _context.Users.Add(user);
        _context.Tenants.Add(tenant);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserAndTenantAsync(userId, tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        result.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsAllMemberships_ForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var user = CreateTestUser(userId, "multi@example.com");
        var tenant1 = CreateTestTenant(tenant1Id, "Tenant 1", "tenant1");
        var tenant2 = CreateTestTenant(tenant2Id, "Tenant 2", "tenant2");
        var membership1 = CreateTestMembership(userId, tenant1Id, "Admin");
        var membership2 = CreateTestMembership(userId, tenant2Id, "Member");

        _context.Users.Add(user);
        _context.Tenants.AddRange(tenant1, tenant2);
        _context.TenantMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.RoleName == "Admin");
        result.Should().Contain(m => m.RoleName == "Member");
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsAllMembers_ForTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var tenant = CreateTestTenant(tenantId, "Test Tenant", "test");
        var user1 = CreateTestUser(user1Id, "user1@example.com");
        var user2 = CreateTestUser(user2Id, "user2@example.com");
        var membership1 = CreateTestMembership(user1Id, tenantId, "Admin");
        var membership2 = CreateTestMembership(user2Id, tenantId, "Member");

        _context.Tenants.Add(tenant);
        _context.Users.AddRange(user1, user2);
        _context.TenantMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTenantIdAsync(tenantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.RoleName == "Admin");
        result.Should().Contain(m => m.RoleName == "Member");
    }

    [Fact]
    public async Task UserHasRoleInTenantAsync_ReturnsTrue_WhenUserHasRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var user = CreateTestUser(userId, "admin@example.com");
        var tenant = CreateTestTenant(tenantId, "Test Tenant", "test");
        var membership = CreateTestMembership(userId, tenantId, "Admin");

        _context.Users.Add(user);
        _context.Tenants.Add(tenant);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UserHasRoleInTenantAsync(userId, tenantId, "Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasRoleInTenantAsync_ReturnsFalse_WhenUserDoesNotHaveRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var user = CreateTestUser(userId, "member@example.com");
        var tenant = CreateTestTenant(tenantId, "Test Tenant", "test");
        var membership = CreateTestMembership(userId, tenantId, "Member");

        _context.Users.Add(user);
        _context.Tenants.Add(tenant);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UserHasRoleInTenantAsync(userId, tenantId, "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPrimaryForUserAsync_ReturnsFirstMembership_WhenMultipleExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var user = CreateTestUser(userId, "primary@example.com");
        var tenant1 = CreateTestTenant(tenant1Id, "First Tenant", "first");
        var tenant2 = CreateTestTenant(tenant2Id, "Second Tenant", "second");

        var firstMembership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenant1Id,
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow.AddDays(-10), // Joined earlier
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var secondMembership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenant2Id,
            RoleName = "Member",
            JoinedAt = DateTime.UtcNow.AddDays(-5), // Joined later
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.Users.Add(user);
        _context.Tenants.AddRange(tenant1, tenant2);
        _context.TenantMemberships.AddRange(firstMembership, secondMembership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPrimaryForUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenant1Id);
        result.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetUserRolesInTenantAsync_ReturnsRoles_ForUserInTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var user = CreateTestUser(userId, "roles@example.com");
        var tenant = CreateTestTenant(tenantId, "Test Tenant", "test");
        var membership = CreateTestMembership(userId, tenantId, "Moderator");

        _context.Users.Add(user);
        _context.Tenants.Add(tenant);
        _context.TenantMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserRolesInTenantAsync(userId, tenantId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Moderator");
    }

    // Helper methods
    private static User CreateTestUser(Guid userId, string email)
    {
        return new User
        {
            Id = userId,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            AuthMethod = AuthenticationMethod.EntraExternalId,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Tenant CreateTestTenant(Guid tenantId, string name, string subdomain)
    {
        return new Tenant
        {
            Id = tenantId,
            Name = name,
            Subdomain = subdomain,
            IsActive = true,
            SubscriptionTier = "Free",
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TenantMembership CreateTestMembership(Guid userId, Guid tenantId, string roleName)
    {
        return new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            RoleName = roleName,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}

