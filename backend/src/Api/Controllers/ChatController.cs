using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OnlineCommunities.Api.Extensions;
using OnlineCommunities.Api.Hubs;
using OnlineCommunities.Core.Entities.Community;
using OnlineCommunities.Core.Interfaces;

namespace OnlineCommunities.Api.Controllers;

/// <summary>
/// REST API controller for chat room management.
/// WebSocket/SignalR connections are handled by ChatHub.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatRoomRepository chatRoomRepository,
        IChatMessageRepository chatMessageRepository,
        IHubContext<ChatHub> hubContext,
        ILogger<ChatController> logger)
    {
        _chatRoomRepository = chatRoomRepository;
        _chatMessageRepository = chatMessageRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all public chat rooms.
    /// </summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetPublicRooms([FromQuery] Guid? tenantId = null)
    {
        var rooms = await _chatRoomRepository.GetPublicChatRoomsAsync(tenantId);

        return Ok(rooms.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.IsPublic,
            r.TenantId,
            r.MaxParticipants,
            MemberCount = r.Members.Count,
            r.CreatedAt
        }));
    }

    /// <summary>
    /// Get all chat rooms the current user is a member of.
    /// </summary>
    [HttpGet("my-rooms")]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var rooms = await _chatRoomRepository.GetUserChatRoomsAsync(userId.Value);

        return Ok(rooms.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.IsPublic,
            r.TenantId,
            MemberCount = r.Members.Count,
            r.CreatedAt
        }));
    }

    /// <summary>
    /// Get a specific chat room with its members.
    /// </summary>
    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRoom(Guid roomId)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var room = await _chatRoomRepository.GetChatRoomWithMembersAsync(roomId);

        if (room == null)
        {
            return NotFound(new { message = "Chat room not found" });
        }

        // Check if user is a member or if the room is public
        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomId, userId.Value);

        if (!isMember && !room.IsPublic)
        {
            return Forbid();
        }

        return Ok(new
        {
            room.Id,
            room.Name,
            room.Description,
            room.IsPublic,
            room.TenantId,
            room.MaxParticipants,
            Members = room.Members.Select(m => new
            {
                m.UserId,
                m.Role,
                m.JoinedAt,
                m.IsMuted
            }),
            IsMember = isMember,
            room.CreatedAt
        });
    }

    /// <summary>
    /// Create a new chat room.
    /// </summary>
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateChatRoomRequest request)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Room name is required" });
        }

        var chatRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic ?? true,
            TenantId = request.TenantId,
            MaxParticipants = request.MaxParticipants,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.Value.ToString()
        };

        await _chatRoomRepository.AddAsync(chatRoom);

        // Add creator as admin member
        await _chatRoomRepository.AddMemberAsync(chatRoom.Id, userId.Value, "admin");

        _logger.LogInformation(
            "User {UserId} created chat room {RoomId} ({RoomName})",
            userId, chatRoom.Id, chatRoom.Name);

        return CreatedAtAction(
            nameof(GetRoom),
            new { roomId = chatRoom.Id },
            new
            {
                chatRoom.Id,
                chatRoom.Name,
                chatRoom.Description,
                chatRoom.IsPublic,
                chatRoom.TenantId,
                chatRoom.CreatedAt
            });
    }

    /// <summary>
    /// Join a public chat room.
    /// </summary>
    [HttpPost("rooms/{roomId}/join")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var room = await _chatRoomRepository.GetByIdAsync(roomId);

        if (room == null)
        {
            return NotFound(new { message = "Chat room not found" });
        }

        if (!room.IsPublic)
        {
            return BadRequest(new { message = "Cannot join private rooms without invitation" });
        }

        // Check if already a member
        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomId, userId.Value);

        if (isMember)
        {
            return BadRequest(new { message = "You are already a member of this room" });
        }

        // Check max participants
        if (room.MaxParticipants.HasValue && room.Members.Count >= room.MaxParticipants.Value)
        {
            return BadRequest(new { message = "Chat room is full" });
        }

        await _chatRoomRepository.AddMemberAsync(roomId, userId.Value);

        _logger.LogInformation("User {UserId} joined chat room {RoomId}", userId, roomId);

        // Notify via SignalR
        await _hubContext.Clients.Group($"ChatRoom_{roomId}").SendAsync("UserJoinedRoom", new
        {
            UserId = userId,
            ChatRoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        return Ok(new { message = "Successfully joined the chat room" });
    }

    /// <summary>
    /// Leave a chat room.
    /// </summary>
    [HttpPost("rooms/{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid roomId)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomId, userId.Value);

        if (!isMember)
        {
            return BadRequest(new { message = "You are not a member of this room" });
        }

        await _chatRoomRepository.RemoveMemberAsync(roomId, userId.Value);

        _logger.LogInformation("User {UserId} left chat room {RoomId}", userId, roomId);

        return Ok(new { message = "Successfully left the chat room" });
    }

    /// <summary>
    /// Get messages from a chat room with pagination.
    /// </summary>
    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid roomId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        // Check if user is a member
        var isMember = await _chatRoomRepository.IsUserMemberAsync(roomId, userId.Value);

        if (!isMember)
        {
            var room = await _chatRoomRepository.GetByIdAsync(roomId);
            if (room == null || !room.IsPublic)
            {
                return Forbid();
            }
        }

        if (take > 100)
        {
            take = 100; // Limit max messages per request
        }

        var messages = await _chatMessageRepository.GetMessagesAsync(roomId, skip, take);

        return Ok(messages.Select(m => new
        {
            m.Id,
            m.ChatRoomId,
            m.UserId,
            m.Content,
            m.MessageType,
            m.ParentMessageId,
            m.IsEdited,
            m.IsDeleted,
            m.CreatedAt
        }));
    }

    /// <summary>
    /// Get unread message count for a chat room.
    /// </summary>
    [HttpGet("rooms/{roomId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(Guid roomId)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var count = await _chatMessageRepository.GetUnreadCountAsync(roomId, userId.Value);

        return Ok(new { roomId, unreadCount = count });
    }
}

// DTOs
public class CreateChatRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsPublic { get; set; } = true;
    public Guid? TenantId { get; set; }
    public int? MaxParticipants { get; set; }
}

