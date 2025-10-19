using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Community;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Infrastructure.Data;

namespace OnlineCommunities.Infrastructure.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public ChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id)
    {
        return await _context.ChatMessages
            .Include(m => m.User)
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<ChatMessage>> GetAllAsync()
    {
        return await _context.ChatMessages
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> AddAsync(ChatMessage entity)
    {
        _context.ChatMessages.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(ChatMessage entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        entity.IsEdited = true;
        _context.ChatMessages.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var message = await GetByIdAsync(id);
        if (message != null)
        {
            message.IsDeleted = true;
            message.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.ChatMessages.AnyAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid chatRoomId, int skip = 0, int take = 50)
    {
        return await _context.ChatMessages
            .Include(m => m.User)
            .Where(m => m.ChatRoomId == chatRoomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAfterAsync(Guid chatRoomId, Guid afterMessageId)
    {
        var afterMessage = await GetByIdAsync(afterMessageId);
        if (afterMessage == null)
        {
            return Enumerable.Empty<ChatMessage>();
        }

        return await _context.ChatMessages
            .Include(m => m.User)
            .Where(m => m.ChatRoomId == chatRoomId && 
                       m.CreatedAt > afterMessage.CreatedAt && 
                       !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid chatRoomId, Guid userId)
    {
        var member = await _context.ChatRoomMembers
            .FirstOrDefaultAsync(m => m.ChatRoomId == chatRoomId && m.UserId == userId);

        if (member == null || !member.LastReadMessageId.HasValue)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ChatRoomId == chatRoomId && !m.IsDeleted);
        }

        var lastReadMessage = await GetByIdAsync(member.LastReadMessageId.Value);
        if (lastReadMessage == null)
        {
            return 0;
        }

        return await _context.ChatMessages
            .CountAsync(m => m.ChatRoomId == chatRoomId && 
                           m.CreatedAt > lastReadMessage.CreatedAt && 
                           !m.IsDeleted);
    }

    public async Task MarkAsReadAsync(Guid chatRoomId, Guid userId, Guid lastReadMessageId)
    {
        var member = await _context.ChatRoomMembers
            .FirstOrDefaultAsync(m => m.ChatRoomId == chatRoomId && m.UserId == userId);

        if (member != null)
        {
            member.LastReadMessageId = lastReadMessageId;
            member.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ChatMessage?> GetMessageWithUserAsync(Guid messageId)
    {
        return await _context.ChatMessages
            .Include(m => m.User)
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
}

