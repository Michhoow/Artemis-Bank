using AutoMapper;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>Servicio generico sobre el repositorio generico. Requerimiento tecnico del proyecto.</summary>
    public class GenericService<TEntity, TDto> : IGenericService<TDto>
        where TEntity : class
        where TDto : class
    {
        protected readonly IGenericRepository<TEntity> Repository;
        protected readonly IMapper Mapper;

        public GenericService(IGenericRepository<TEntity> repository, IMapper mapper)
        {
            Repository = repository;
            Mapper = mapper;
        }

        public virtual async Task<TDto?> AddAsync(TDto dto)
        {
            var entity = Mapper.Map<TEntity>(dto);
            var saved = await Repository.AddAsync(entity);
            return Mapper.Map<TDto>(saved);
        }

        public virtual async Task<TDto?> UpdateAsync(TDto dto, int id)
        {
            var entity = Mapper.Map<TEntity>(dto);
            var updated = await Repository.UpdateAsync(id, entity);
            return updated == null ? null : Mapper.Map<TDto>(updated);
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await Repository.GetByIdAsync(id);
            if (entity == null) return false;
            await Repository.DeleteAsync(id);
            return true;
        }

        public virtual async Task<TDto?> GetByIdAsync(int id)
        {
            var entity = await Repository.GetByIdAsync(id);
            return entity == null ? null : Mapper.Map<TDto>(entity);
        }

        public virtual async Task<List<TDto>> GetAllAsync()
        {
            var list = await Repository.GetAllListAsync();
            return Mapper.Map<List<TDto>>(list);
        }
    }
}
