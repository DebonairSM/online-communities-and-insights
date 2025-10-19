using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Entities.Community;

/// <summary>
/// Represents a chat room where users can have real-time conversations.
/// Chat rooms can be public, private, or community-specific.
/// </summary>
public class ChatRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// The tenant/community this chat room belongs to.
    /// Null for global chat rooms.
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// Whether this is a public chat room (anyone can join) or private (invite only).
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// Whether this chat room is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Maximum number of participants allowed in this chat room.
    /// Null means unlimited.
    /// </summary>
    public int? MaxParticipants { get; set; }
    
    /// <summary>
    /// Navigation property: Messages in this chat room.
    /// </summary>
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    
    /// <summary>
    /// Navigation property: Users who are members of this chat room.
    /// </summary>
    public ICollection<ChatRoomMember> Members { get; set; } = new List<ChatRoomMember>();
}

