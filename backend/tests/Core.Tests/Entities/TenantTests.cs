using FluentAssertions;
using OnlineCommunities.Core.Entities.Tenants;
using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void Tenant_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Acme Research",
            Subdomain = "acme",
            IsActive = true,
            SubscriptionTier = "Free",
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Assert
        tenant.Should().NotBeNull();
        tenant.Name.Should().Be("Acme Research");
        tenant.Subdomain.Should().Be("acme");
        tenant.IsActive.Should().BeTrue();
        tenant.SubscriptionTier.Should().Be("Free");
    }

    [Theory]
    [InlineData("Free")]
    [InlineData("Professional")]
    [InlineData("Enterprise")]
    public void Tenant_SupportsSubscriptionTiers(string tier)
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            Subdomain = "testco",
            SubscriptionTier = tier,
            IsActive = true
        };

        // Assert
        tenant.SubscriptionTier.Should().Be(tier);
    }

    [Fact]
    public void Tenant_HasMembersCollection()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Subdomain = "test",
            IsActive = true
        };

        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = tenant.Id,
            RoleName = "Admin",
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        tenant.Members.Add(membership);

        // Assert
        tenant.Members.Should().HaveCount(1);
        tenant.Members.Should().Contain(membership);
    }

    [Fact]
    public void Tenant_CanStoreSettingsAsJson()
    {
        // Arrange & Act
        var settings = "{\"theme\":\"dark\",\"features\":[\"surveys\",\"polls\"]}";
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Configured Tenant",
            Subdomain = "configured",
            IsActive = true,
            Settings = settings
        };

        // Assert
        tenant.Settings.Should().NotBeNull();
        tenant.Settings.Should().Contain("theme");
        tenant.Settings.Should().Contain("features");
    }

    [Fact]
    public void Tenant_SubdomainIsRequired()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            Subdomain = string.Empty,
            IsActive = true
        };

        // Assert
        tenant.Subdomain.Should().NotBeNull();
        tenant.Subdomain.Should().BeEmpty();
    }

    [Fact]
    public void Tenant_InitializedWithEmptyMembersCollection()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "New Tenant",
            Subdomain = "new",
            IsActive = true
        };

        // Assert
        tenant.Members.Should().NotBeNull();
        tenant.Members.Should().BeEmpty();
    }
}

