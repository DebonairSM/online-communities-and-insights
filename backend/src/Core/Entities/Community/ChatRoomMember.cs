using OnlineCommunities.Core.Entities.Common;
using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Entities.Community;

/// <summary>
/// Represents a user's membership in a chat room.
/// Tracks when they joined and their last read message.
/// </summary>
public class ChatRoomMember : BaseEntity
{
    /// <summary>
    /// The chat room ID.
    /// </summary>
    public Guid ChatRoomId { get; set; }
    
    /// <summary>
    /// Navigation property: The chat room.
    /// </summary>
    public ChatRoom ChatRoom { get; set; } = null!;
    
    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property: The user.
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// When the user joined this chat room.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The last message ID that this user has read.
    /// Used to track unread messages.
    /// </summary>
    public Guid? LastReadMessageId { get; set; }
    
    /// <summary>
    /// When the user last read messages in this chat room.
    /// </summary>
    public DateTime? LastReadAt { get; set; }
    
    /// <summary>
    /// Whether the user has muted notifications for this chat room.
    /// </summary>
    public bool IsMuted { get; set; }
    
    /// <summary>
    /// User's role in this chat room (admin, moderator, member).
    /// </summary>
    public string Role { get; set; } = "member";
}

