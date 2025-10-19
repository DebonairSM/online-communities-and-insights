using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineCommunities.Api.Extensions;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Core.Entities.Community;

namespace OnlineCommunities.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time chat functionality.
/// Handles WebSocket connections for sending and receiving chat messages.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatRoomRepository chatRoomRepository,
        IChatMessageRepository chatMessageRepository,
        ILogger<ChatHub> logger)
    {
        _chatRoomRepository = chatRoomRepository;
        _chatMessageRepository = chatMessageRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();
        var email = Context.User?.GetEmail();

        if (userId.HasValue)
        {
            // Add user to their personal group for direct messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");

            _logger.LogInformation(
                "User {Email} ({UserId}) connected to ChatHub with connection {ConnectionId}",
                email, userId.Value, Context.ConnectionId);

            // Notify the user they're connected
            await Clients.Caller.SendAsync("Connected", new
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId.Value,
                Timestamp = DateTime.UtcNow
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.GetUserId();

        if (userId.HasValue)
        {
            _logger.LogInformation(
                "User {UserId} disconnected from ChatHub (Connection: {ConnectionId})",
                userId.Value, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a chat room. User must be a member to join.
    /// </summary>
    public async Task JoinRoom(string chatRoomId)
    {
        var userId = Context.User?.GetUserId();

        if (!userId.HasValue || !Guid.TryParse(chatRoomId, out var roomGuid))
        {
            await Clients.Caller.SendAsync("Error", new { Message = "Invalid request" });
            return;
        }

        // Check if user is a member of this chat room
        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomGuid, userId.Value);

        if (!isMember)
        {
            await Clients.Caller.SendAsync("Error", new { Message = "You are not a member of this chat room" });
            return;
        }

        // Add connection to the chat room group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");

        _logger.LogInformation("User {UserId} joined chat room {ChatRoomId}", userId.Value, chatRoomId);

        // Notify the room that a user joined
        await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserJoined", new
        {
            UserId = userId.Value,
            ChatRoomId = chatRoomId,
            Timestamp = DateTime.UtcNow
        });

        // Confirm to the caller
        await Clients.Caller.SendAsync("JoinedRoom", new
        {
            ChatRoomId = chatRoomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leave a chat room.
    /// </summary>
    public async Task LeaveRoom(string chatRoomId)
    {
        var userId = Context.User?.GetUserId();

        if (!userId.HasValue)
        {
            return;
        }

        // Remove connection from the chat room group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");

        _logger.LogInformation("User {UserId} left chat room {ChatRoomId}", userId.Value, chatRoomId);

        // Notify the room that a user left
        await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("UserLeft", new
        {
            UserId = userId.Value,
            ChatRoomId = chatRoomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send a message to a chat room.
    /// Message is saved to database and broadcast to all room members.
    /// </summary>
    public async Task SendMessage(string chatRoomId, string message)
    {
        var userId = Context.User?.GetUserId();
        var email = Context.User?.GetEmail();

        if (!userId.HasValue || !Guid.TryParse(chatRoomId, out var roomGuid))
        {
            await Clients.Caller.SendAsync("Error", new { Message = "Invalid request" });
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            await Clients.Caller.SendAsync("Error", new { Message = "Message cannot be empty" });
            return;
        }

        // Check if user is a member
        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomGuid, userId.Value);

        if (!isMember)
        {
            await Clients.Caller.SendAsync("Error", new { Message = "You are not a member of this chat room" });
            return;
        }

        // Create and save the message
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatRoomId = roomGuid,
            UserId = userId.Value,
            Content = message,
            MessageType = "text",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.Value.ToString()
        };

        await _chatMessageRepository.AddAsync(chatMessage);

        _logger.LogInformation(
            "User {Email} sent message to chat room {ChatRoomId}",
            email, chatRoomId);

        // Broadcast message to all users in the room
        await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
        {
            Id = chatMessage.Id,
            ChatRoomId = chatRoomId,
            UserId = userId.Value,
            UserEmail = email,
            Content = message,
            MessageType = "text",
            Timestamp = chatMessage.CreatedAt,
            IsEdited = false
        });
    }

    /// <summary>
    /// Send typing indicator to other users in the chat room.
    /// </summary>
    public async Task SendTypingIndicator(string chatRoomId, bool isTyping)
    {
        var userId = Context.User?.GetUserId();
        var email = Context.User?.GetEmail();

        if (!userId.HasValue)
        {
            return;
        }

        // Broadcast typing indicator to others in the room (not to self)
        await Clients.OthersInGroup($"ChatRoom_{chatRoomId}").SendAsync("UserTyping", new
        {
            UserId = userId.Value,
            UserEmail = email,
            ChatRoomId = chatRoomId,
            IsTyping = isTyping,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Mark messages as read in a chat room.
    /// </summary>
    public async Task MarkAsRead(string chatRoomId, string lastMessageId)
    {
        var userId = Context.User?.GetUserId();

        if (!userId.HasValue || 
            !Guid.TryParse(chatRoomId, out var roomGuid) ||
            !Guid.TryParse(lastMessageId, out var messageGuid))
        {
            return;
        }

        await _chatMessageRepository.MarkAsReadAsync(roomGuid, userId.Value, messageGuid);

        _logger.LogInformation(
            "User {UserId} marked messages as read in chat room {ChatRoomId} up to message {MessageId}",
            userId.Value, chatRoomId, lastMessageId);
    }
}

