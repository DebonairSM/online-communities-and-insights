using OnlineCommunities.Core.Entities.Common;
using OnlineCommunities.Core.Entities.Identity;

namespace OnlineCommunities.Core.Entities.Community;

/// <summary>
/// Represents a message sent in a chat room.
/// </summary>
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// The chat room this message belongs to.
    /// </summary>
    public Guid ChatRoomId { get; set; }
    
    /// <summary>
    /// Navigation property: The chat room this message belongs to.
    /// </summary>
    public ChatRoom ChatRoom { get; set; } = null!;
    
    /// <summary>
    /// The user who sent this message.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property: The user who sent this message.
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// The content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of message: text, image, file, system, etc.
    /// </summary>
    public string MessageType { get; set; } = "text";
    
    /// <summary>
    /// If this message is a reply to another message, this is the parent message ID.
    /// </summary>
    public Guid? ParentMessageId { get; set; }
    
    /// <summary>
    /// Whether this message has been edited.
    /// </summary>
    public bool IsEdited { get; set; }
    
    /// <summary>
    /// Whether this message has been deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Additional metadata for the message (URLs, mentions, etc.)
    /// Stored as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

