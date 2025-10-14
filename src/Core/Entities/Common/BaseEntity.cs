namespace OnlineCommunities.Core.Entities.Common;

/// <summary>
/// Base class for all entities in the domain.
/// Provides common properties like Id, Created/Modified timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

