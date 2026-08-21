using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface IBeneficiaryRepository : IGenericRepository<Beneficiary>
    {
        Task<List<Beneficiary>> GetByClientAsync(string clientId);
        Task<Beneficiary?> GetByClientAndAccountAsync(string clientId, int savingsAccountId);
        Task<bool> ExistsAsync(string clientId, int savingsAccountId);
    }
}
