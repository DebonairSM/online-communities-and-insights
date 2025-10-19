using OnlineCommunities.Core.Entities.Common;

namespace OnlineCommunities.Core.Interfaces;

/// <summary>
/// Generic repository interface for CRUD operations.
/// This is the contract that Infrastructure layer will implement.
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

