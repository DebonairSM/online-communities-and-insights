using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace OnlineCommunities.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity.
/// Supports queries for all authentication methods.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Find a user by their email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Find a user by external OAuth login (legacy - no longer used).
    /// </summary>
    public async Task<User?> GetByExternalLoginAsync(string provider, string externalUserId)
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => 
                u.ExternalLoginProvider == provider && 
                u.ExternalUserId == externalUserId);
    }

    /// <summary>
    /// Find a user by their Entra ID subject identifier.
    /// Used during JIT provisioning for Microsoft Entra External ID authentication.
    /// </summary>
    public async Task<User?> GetByEntraIdSubjectAsync(string entraIdSubject)
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => u.EntraIdSubject == entraIdSubject);
    }

    /// <summary>
    /// Get all tenant IDs that a user belongs to.
    /// </summary>
    public async Task<IEnumerable<Guid>> GetUserTenantIdsAsync(Guid userId)
    {
        return await _context.TenantMemberships
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TenantId)
            .ToListAsync();
    }

    /// <summary>
    /// Check if a user is a member of a specific tenant.
    /// </summary>
    public async Task<bool> IsMemberOfTenantAsync(Guid userId, Guid tenantId)
    {
        return await _context.TenantMemberships
            .AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId);
    }

    // IRepository<User> base methods
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .ToListAsync();
    }

    public async Task<User> AddAsync(User entity)
    {
        _context.Users.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(User entity)
    {
        _context.Users.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEntraOidAsync(string entraOid)
    {
        return await _context.Users
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => u.EntraIdSubject == entraOid);
    }
}

