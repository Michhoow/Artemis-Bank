namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>Servicio generico. Requerimiento tecnico del proyecto.</summary>
    public interface IGenericService<TDto> where TDto : class
    {
        Task<TDto?> AddAsync(TDto dto);
        Task<TDto?> UpdateAsync(TDto dto, int id);
        Task<bool> DeleteAsync(int id);
        Task<TDto?> GetByIdAsync(int id);
        Task<List<TDto>> GetAllAsync();
    }
}
