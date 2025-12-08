using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SkillSync.Data.Entities;
using System.Linq.Expressions;

namespace SkillSync.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }


        public IIncludableQueryable<T, TProperty> Include<TProperty>(
            Expression<Func<T, TProperty>> includeExpression)
        {
            return _dbSet.Include(includeExpression);
        }

        public IIncludableQueryable<T, TProperty> ThenInclude<TPreviousProperty, TProperty>(
            IIncludableQueryable<T, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        {
            return source.ThenInclude(navigationPropertyPath);
        }

        public IQueryable<T> GetAll()
        {
            return _dbSet.AsQueryable();
        }



        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking()
                               .Where(predicate)
                               .ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}