using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    /// <summary>
    /// Repositorio para la gestión de comercios.
    /// Propiedad: Monserrat.
    /// </summary>
    public interface ICommerceRepository : IGenericRepository<Commerce>
    {
        Task<Commerce?> GetByRncAsync(string rnc, CancellationToken cancellationToken = default);
        Task<Commerce?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> RncExistsAsync(string rnc, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    }
}
