using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Infrastructure.Data;

namespace OnlineCommunities.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TenantMembership entity.
/// Handles tenant role queries for authorization.
/// </summary>
public class TenantMembershipRepository : ITenantMembershipRepository
{
    private readonly ApplicationDbContext _context;

    public TenantMembershipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantMembership?> GetByIdAsync(Guid id)
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .FirstOrDefaultAsync(tm => tm.Id == id);
    }

    public async Task<IEnumerable<TenantMembership>> GetAllAsync()
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .ToListAsync();
    }

    public async Task<TenantMembership> AddAsync(TenantMembership entity)
    {
        _context.TenantMemberships.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(TenantMembership entity)
    {
        _context.TenantMemberships.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.TenantMemberships.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.TenantMemberships.AnyAsync(tm => tm.Id == id);
    }

    public async Task<TenantMembership?> GetByUserAndTenantAsync(Guid userId, Guid tenantId)
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TenantId == tenantId);
    }

    public async Task<IEnumerable<TenantMembership>> GetByUserIdAsync(Guid userId)
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .Where(tm => tm.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TenantMembership>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .Where(tm => tm.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> UserHasRoleInTenantAsync(Guid userId, Guid tenantId, string roleName)
    {
        return await _context.TenantMemberships
            .AnyAsync(tm => tm.UserId == userId && 
                          tm.TenantId == tenantId && 
                          tm.RoleName == roleName);
    }

    public async Task<List<string>> GetUserRolesInTenantAsync(Guid userId, Guid tenantId)
    {
        return await _context.TenantMemberships
            .Where(tm => tm.UserId == userId && tm.TenantId == tenantId)
            .Select(tm => tm.RoleName)
            .ToListAsync();
    }

    public async Task<TenantMembership?> GetPrimaryForUserAsync(Guid userId)
    {
        return await _context.TenantMemberships
            .Include(tm => tm.User)
            .Include(tm => tm.Tenant)
            .Where(tm => tm.UserId == userId)
            .OrderBy(tm => tm.JoinedAt)
            .FirstOrDefaultAsync();
    }
}
