using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ISavingsAccountRepository : IGenericRepository<SavingsAccount>
    {
        Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber);
        Task<SavingsAccount?> GetPrincipalByClientAsync(string clientId);
        Task<List<SavingsAccount>> GetActiveByClientAsync(string clientId);
        Task<bool> AccountNumberExistsAsync(string accountNumber);
        Task<int> CountActiveAsync();
        Task<decimal> GetTotalBalanceByClientAsync(string clientId);
    }
}
