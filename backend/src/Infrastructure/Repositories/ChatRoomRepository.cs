using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Community;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Infrastructure.Data;

namespace OnlineCommunities.Infrastructure.Repositories;

public class ChatRoomRepository : IChatRoomRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRoomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatRoom?> GetByIdAsync(Guid id)
    {
        return await _context.ChatRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<ChatRoom>> GetAllAsync()
    {
        return await _context.ChatRooms
            .Include(r => r.Members)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatRoom> AddAsync(ChatRoom entity)
    {
        _context.ChatRooms.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(ChatRoom entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        _context.ChatRooms.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var chatRoom = await GetByIdAsync(id);
        if (chatRoom != null)
        {
            chatRoom.IsActive = false;
            chatRoom.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.ChatRooms.AnyAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<ChatRoom>> GetPublicChatRoomsAsync(Guid? tenantId = null)
    {
        var query = _context.ChatRooms
            .Include(r => r.Members)
            .Where(r => r.IsPublic && r.IsActive);

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoom>> GetUserChatRoomsAsync(Guid userId)
    {
        return await _context.ChatRooms
            .Include(r => r.Members)
            .Where(r => r.Members.Any(m => m.UserId == userId) && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatRoom?> GetChatRoomWithMembersAsync(Guid chatRoomId)
    {
        return await _context.ChatRooms
            .Include(r => r.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == chatRoomId);
    }

    public async Task<bool> IsUserMemberAsync(Guid chatRoomId, Guid userId)
    {
        return await _context.ChatRoomMembers
            .AnyAsync(m => m.ChatRoomId == chatRoomId && m.UserId == userId);
    }

    public async Task<ChatRoomMember> AddMemberAsync(Guid chatRoomId, Guid userId, string role = "member")
    {
        var member = new ChatRoomMember
        {
            Id = Guid.NewGuid(),
            ChatRoomId = chatRoomId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatRoomMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task RemoveMemberAsync(Guid chatRoomId, Guid userId)
    {
        var member = await _context.ChatRoomMembers
            .FirstOrDefaultAsync(m => m.ChatRoomId == chatRoomId && m.UserId == userId);

        if (member != null)
        {
            _context.ChatRoomMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}

