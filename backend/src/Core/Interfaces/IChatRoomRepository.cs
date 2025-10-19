using OnlineCommunities.Core.Entities.Community;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Repository interface for ChatRoom entity operations.
/// </summary>
public interface IChatRoomRepository : IRepository<ChatRoom>
{
    /// <summary>
    /// Get all public chat rooms, optionally filtered by tenant.
    /// </summary>
    Task<IEnumerable<ChatRoom>> GetPublicChatRoomsAsync(Guid? tenantId = null);
    
    /// <summary>
    /// Get all chat rooms a user is a member of.
    /// </summary>
    Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(Guid userId);
    
    /// <summary>
    /// Get a chat room with its members.
    /// </summary>
    Task<ChatRoom?> GetChatRoomWithMembersAsync(Guid chatRoomId);
    
    /// <summary>
    /// Check if a user is a member of a chat room.
    /// </summary>
    Task<bool> IsUserMemberAsync(Guid chatRoomId, Guid userId);
    
    /// <summary>
    /// Add a user to a chat room.
    /// </summary>
    Task<ChatRoomMember> AddMemberAsync(Guid chatRoomId, Guid userId, string role = "member");
    
    /// <summary>
    /// Remove a user from a chat room.
    /// </summary>
    Task RemoveMemberAsync(Guid chatRoomId, Guid userId);
}

