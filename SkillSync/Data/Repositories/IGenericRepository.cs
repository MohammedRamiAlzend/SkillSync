using System.Linq.Expressions;

namespace SkillSync.Data.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> GetAll();
        Task<List<T>> GetAllAsync();

        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<T?> GetByIdAsync(int id);

        IQueryable<T> GetAllIncluding(params Expression<Func<T, object>>[] includeProperties);

        Task<T?> GetByIdIncludingAsync(int id, params Expression<Func<T, object>>[] includeProperties);

        Task AddAsync(T entity);

        Task AddRangeAsync(IEnumerable<T> entities);

        void Update(T entity);

        void Remove(T entity);

        Task<int> SaveChangesAsync();

        Task<T?> GetFirstIncludingAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties);

    }
}
