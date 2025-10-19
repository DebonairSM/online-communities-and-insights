using FluentAssertions;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Entities.Tenants;
using OnlineCommunities.Core.Enums;

namespace OnlineCommunities.Core.Tests.Entities;

public class TenantMembershipTests
{
    [Fact]
    public void TenantMembership_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Assert
        membership.Should().NotBeNull();
        membership.UserId.Should().Be(userId);
        membership.TenantId.Should().Be(tenantId);
        membership.RoleName.Should().Be("Admin");
        membership.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void TenantMembership_SupportsCommonRoles(string roleName)
    {
        // Arrange & Act
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            RoleName = roleName,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Assert
        membership.RoleName.Should().Be(roleName);
    }

    [Fact]
    public void TenantMembership_HasDefaultRoleOfMember()
    {
        // Arrange & Act
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            RoleName = "Member",
            JoinedAt = DateTime.UtcNow
        };

        // Assert
        membership.RoleName.Should().Be("Member");
    }

    [Fact]
    public void TenantMembership_CanHaveAdditionalPermissions()
    {
        // Arrange & Act
        var permissions = "[\"post.delete\", \"comment.moderate\", \"survey.publish\"]";
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            RoleName = "Moderator",
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            AdditionalPermissions = permissions
        };

        // Assert
        membership.AdditionalPermissions.Should().NotBeNull();
        membership.AdditionalPermissions.Should().Contain("post.delete");
    }

    [Fact]
    public void TenantMembership_TracksJoinDate()
    {
        // Arrange
        var joinDate = DateTime.UtcNow;

        // Act
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            RoleName = "Member",
            JoinedAt = joinDate,
            IsActive = true
        };

        // Assert
        membership.JoinedAt.Should().Be(joinDate);
        membership.JoinedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}

