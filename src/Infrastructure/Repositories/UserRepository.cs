using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OnlineCommunities.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity.
/// Supports queries for all authentication methods.
/// </summary>
public class UserRepository : IUserRepository
{
    // TODO: Inject your DbContext here
    // private readonly ApplicationDbContext _context;

    // public UserRepository(ApplicationDbContext context)
    // {
    //     _context = context;
    // }

    /// <summary>
    /// Find a user by their email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        // TODO: Implement actual database query
        // return await _context.Users
        //     .Include(u => u.TenantMemberships)
        //     .FirstOrDefaultAsync(u => u.Email == email);

        throw new NotImplementedException(
            "TODO: Implement GetByEmailAsync - query Users table by Email column");
    }

    /// <summary>
    /// Find a user by external OAuth login (Phase 2: Social Login).
    /// This is the PRIMARY lookup method for social login authentication.
    /// </summary>
    public async Task<User?> GetByExternalLoginAsync(string provider, string externalUserId)
    {
        // TODO: Implement actual database query
        // return await _context.Users
        //     .Include(u => u.TenantMemberships)
        //     .FirstOrDefaultAsync(u => 
        //         u.ExternalLoginProvider == provider && 
        //         u.ExternalUserId == externalUserId);

        throw new NotImplementedException(
            "TODO: Implement GetByExternalLoginAsync - query Users table by ExternalLoginProvider and ExternalUserId");
    }

    /// <summary>
    /// Find a user by their Entra ID subject identifier (Phase 3: Enterprise SSO).
    /// Used during JIT provisioning for enterprise SSO.
    /// </summary>
    public async Task<User?> GetByEntraIdSubjectAsync(string entraIdSubject)
    {
        // TODO: Implement actual database query
        // return await _context.Users
        //     .Include(u => u.TenantMemberships)
        //     .FirstOrDefaultAsync(u => u.EntraIdSubject == entraIdSubject);

        throw new NotImplementedException(
            "TODO: Implement GetByEntraIdSubjectAsync - query Users table by EntraIdSubject column");
    }

    /// <summary>
    /// Get all tenant IDs that a user belongs to.
    /// </summary>
    public async Task<IEnumerable<Guid>> GetUserTenantIdsAsync(Guid userId)
    {
        // TODO: Implement actual database query
        // return await _context.TenantMemberships
        //     .Where(tm => tm.UserId == userId)
        //     .Select(tm => tm.TenantId)
        //     .ToListAsync();

        throw new NotImplementedException(
            "TODO: Implement GetUserTenantIdsAsync - query TenantMemberships table");
    }

    /// <summary>
    /// Check if a user is a member of a specific tenant.
    /// </summary>
    public async Task<bool> IsMemberOfTenantAsync(Guid userId, Guid tenantId)
    {
        // TODO: Implement actual database query
        // return await _context.TenantMemberships
        //     .AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId);

        throw new NotImplementedException(
            "TODO: Implement IsMemberOfTenantAsync - check TenantMemberships table");
    }

    // TODO: Implement IRepository<User> base methods
    public async Task<User?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException("TODO: Implement GetByIdAsync");
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        throw new NotImplementedException("TODO: Implement GetAllAsync");
    }

    public async Task AddAsync(User entity)
    {
        // TODO: Implement actual database insert
        // _context.Users.Add(entity);
        // await _context.SaveChangesAsync();
        
        throw new NotImplementedException(
            "TODO: Implement AddAsync - insert new User into database");
    }

    public async Task UpdateAsync(User entity)
    {
        // TODO: Implement actual database update
        // _context.Users.Update(entity);
        // await _context.SaveChangesAsync();
        
        throw new NotImplementedException(
            "TODO: Implement UpdateAsync - update existing User in database");
    }

    public async Task DeleteAsync(Guid id)
    {
        // TODO: Implement actual database delete (or soft delete)
        // var user = await GetByIdAsync(id);
        // if (user != null)
        // {
        //     _context.Users.Remove(user);
        //     await _context.SaveChangesAsync();
        // }
        
        throw new NotImplementedException(
            "TODO: Implement DeleteAsync - delete or soft-delete User");
    }
}

