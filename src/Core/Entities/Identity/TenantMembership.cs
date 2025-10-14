using OnlineCommunities.Core.Entities.Common;
using OnlineCommunities.Core.Entities.Tenants;

namespace OnlineCommunities.Core.Entities.Identity;

/// <summary>
/// Represents a user's membership in a tenant with associated roles.
/// This is the many-to-many relationship between Users and Tenants.
/// CRITICAL: Roles are stored HERE in YOUR database, NOT in Entra ID!
/// </summary>
public class TenantMembership : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// The role name for this user in this tenant.
    /// Examples: "Admin", "Moderator", "Member"
    /// These are YOUR application roles, not Entra ID roles.
    /// </summary>
    public string RoleName { get; set; } = "Member";

    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional: Additional permissions beyond the base role.
    /// Stored as JSON array of permission strings.
    /// </summary>
    public string? AdditionalPermissions { get; set; }
}

