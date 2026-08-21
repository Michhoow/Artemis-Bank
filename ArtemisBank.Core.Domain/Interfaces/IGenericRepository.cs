namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        Task<List<TEntity>> AddRangeAsync(List<TEntity> entities);
        Task<TEntity?> UpdateAsync(int id, TEntity entity);
        Task UpdateEntityAsync(TEntity entity);
        Task DeleteAsync(int id);
        Task<TEntity?> GetByIdAsync(int id);
        Task<List<TEntity>> GetAllListAsync();
        IQueryable<TEntity> GetAllQuery();
        IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties);
        Task<List<TEntity>> GetAllListWithIncludeAsync(List<string> properties);
    }
}
