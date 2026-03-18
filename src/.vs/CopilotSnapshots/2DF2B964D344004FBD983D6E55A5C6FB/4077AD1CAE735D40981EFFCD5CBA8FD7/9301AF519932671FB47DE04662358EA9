using System.Linq.Expressions;
using App.Core.Entities.Commons;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Repositories.Abstractions
{
    public class Repository<T> : IRepository<T>, IDisposable where T : BaseEntity
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<T> DbSet;
        private bool _disposed = false;

        protected Repository(AppDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await DbSet.FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[]? includes)
        {
            var query = DbSet.AsQueryable();
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return await query.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<ICollection<T>> GetAllAsync(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {
            var query = DbSet.Where(predicate);
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return await query.ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await DbSet.AddAsync(entity);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            DbSet.Update(entity);
            await Task.CompletedTask;
        }

        public virtual async Task SoftDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                DbSet.Update(entity);
            }
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await DbSet.AnyAsync(e => e.Id == id);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null
                ? await DbSet.CountAsync()
                : await DbSet.CountAsync(predicate);
        }

        public virtual async Task<T> RemoveAsync(T entity)
        {
            DbSet.Remove(entity);
            await Task.CompletedTask;
            return entity;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
