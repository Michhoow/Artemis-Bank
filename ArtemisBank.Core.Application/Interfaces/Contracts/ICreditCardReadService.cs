using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    public interface ICreditCardReadService
    {
        Task<CreditCardInfoDto?> GetByIdAsync(int creditCardId);

        Task<CreditCardInfoDto?> GetByCardNumberAsync(string cardNumber);

        Task<List<CreditCardInfoDto>> GetActiveByClientAsync(string clientId);
        Task<int> CountActiveAsync();
        Task<decimal> GetDebtByClientAsync(string clientId);

        Task ApplyPaymentAsync(int creditCardId, decimal effectiveAmount, CancellationToken cancellationToken = default);
    }
}
