using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    /// <summary>
    /// CONTRATO CONGELADO — lectura de usuarios de Identity.
    /// IMPLEMENTA: Monserrat en ArtemisBank.Infrastructure.Identity.
    /// CONSUME: Michael (cuentas, transacciones, cajero, home admin) y Manuel.
    /// </summary>
    public interface IUserReadService
    {
        Task<UserInfoDto?> GetByIdAsync(string userId);
        Task<UserInfoDto?> GetByIdentificationAsync(string identification);
        Task<List<UserInfoDto>> GetByIdsAsync(IEnumerable<string> userIds);
        Task<List<UserInfoDto>> GetClientsAsync(bool onlyActive = true);
        Task<int> CountClientsAsync(bool? isActive = null);
        Task<List<string>> GetActiveClientIdsAsync();
    }
}
