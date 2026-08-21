using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ICreditCardRepository : IGenericRepository<CreditCard>
    {
        Task<CreditCard?> GetWithConsumptionsAsync(int creditCardId, CancellationToken cancellationToken = default);

        Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);

        Task<List<CreditCard>> GetActiveByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<List<CreditCard>> GetAllByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        Task<decimal> GetDebtByClientAsync(string clientId, CancellationToken cancellationToken = default);

        IQueryable<CreditCard> Query();
    }
}
