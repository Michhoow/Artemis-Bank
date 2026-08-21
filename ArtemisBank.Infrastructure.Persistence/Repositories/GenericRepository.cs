using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly ArtemisDbContext Context;

        public GenericRepository(ArtemisDbContext context) => Context = context;

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await Context.Set<TEntity>().AddAsync(entity);
            await Context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> entities)
        {
            await Context.Set<TEntity>().AddRangeAsync(entities);
            await Context.SaveChangesAsync();
            return entities;
        }

        public virtual async Task<TEntity?> UpdateAsync(int id, TEntity entity)
        {
            var current = await Context.Set<TEntity>().FindAsync(id);
            if (current == null) return null;

            Context.Entry(current).CurrentValues.SetValues(entity);
            await Context.SaveChangesAsync();
            return current;
        }

        public virtual async Task UpdateEntityAsync(TEntity entity)
        {
            Context.Set<TEntity>().Update(entity);
            await Context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await Context.Set<TEntity>().FindAsync(id);
            if (entity == null) return;

            Context.Set<TEntity>().Remove(entity);
            await Context.SaveChangesAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id)
            => await Context.Set<TEntity>().FindAsync(id);

        public virtual async Task<List<TEntity>> GetAllListAsync()
            => await Context.Set<TEntity>().ToListAsync();

        public virtual IQueryable<TEntity> GetAllQuery()
            => Context.Set<TEntity>().AsQueryable();

        public virtual IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties)
        {
            var query = Context.Set<TEntity>().AsQueryable();
            foreach (var property in properties) query = query.Include(property);
            return query;
        }

        public virtual async Task<List<TEntity>> GetAllListWithIncludeAsync(List<string> properties)
            => await GetAllQueryWithInclude(properties).ToListAsync();
    }
}
