using OnlineCommunities.Core.Entities.Common;
using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Entities.Tenants;

/// <summary>
/// Represents a tenant (customer organization) in the multi-tenant SaaS.
/// Each tenant has its own isolated data and users with roles.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty; // e.g., "acmecorp" for acmecorp.yoursaas.com
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Subscription tier: "Free", "Professional", "Enterprise"
    /// </summary>
    public string SubscriptionTier { get; set; } = "Free";
    
    public DateTime SubscriptionExpiresAt { get; set; }
    
    /// <summary>
    /// Configuration settings stored as JSON.
    /// Includes theme colors, feature flags, etc.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Navigation property: All users who are members of this tenant.
    /// </summary>
    public ICollection<TenantMembership> Members { get; set; } = new List<TenantMembership>();
}

