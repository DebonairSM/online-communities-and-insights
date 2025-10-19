using OnlineCommunities.Core.Entities.Community;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Repository interface for ChatMessage entity operations.
/// </summary>
public interface IChatMessageRepository : IRepository<ChatMessage>
{
    /// <summary>
    /// Get messages for a chat room with pagination.
    /// </summary>
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid chatRoomId, int skip = 0, int take = 50);
    
    /// <summary>
    /// Get messages after a specific message ID (for real-time updates).
    /// </summary>
    Task<IEnumerable<ChatMessage>> GetMessagesAfterAsync(Guid chatRoomId, Guid afterMessageId);
    
    /// <summary>
    /// Get the count of unread messages for a user in a chat room.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid chatRoomId, Guid userId);
    
    /// <summary>
    /// Mark messages as read for a user.
    /// </summary>
    Task MarkAsReadAsync(Guid chatRoomId, Guid userId, Guid lastReadMessageId);
    
    /// <summary>
    /// Get a message with user information.
    /// </summary>
    Task<ChatMessage?> GetMessageWithUserAsync(Guid messageId);
}

