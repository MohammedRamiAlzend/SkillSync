using System.Linq.Expressions;

namespace SkillSync.Data.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        // Read All
        Task<List<T>> GetAllAsync();

        // Read With filters
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Read Id ( PK int)
        Task<T?> GetByIdAsync(int id);

        // Add new item
        Task AddAsync(T entity);

        // Add 2 or more items
        Task AddRangeAsync(IEnumerable<T> entities);

        // Update item
        void Update(T entity);

        // Delete item
        void Remove(T entity);

        // Save changes
        Task<int> SaveChangesAsync();
    }
}
