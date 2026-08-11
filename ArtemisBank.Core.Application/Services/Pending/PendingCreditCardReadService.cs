using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services.Pending
{
    /// <summary>
    /// TEMPORAL — implementacion puente de ICreditCardReadService.
    /// La definitiva la provee Manuel en el modulo de tarjetas de credito.
    /// </summary>
    public class PendingCreditCardReadService : ICreditCardReadService
    {
        private readonly ILogger<PendingCreditCardReadService> _logger;

        public PendingCreditCardReadService(ILogger<PendingCreditCardReadService> logger) => _logger = logger;

        public Task<CreditCardInfoDto?> GetByIdAsync(int creditCardId) => Task.FromResult<CreditCardInfoDto?>(null);

        public Task<CreditCardInfoDto?> GetByCardNumberAsync(string cardNumber)
            => Task.FromResult<CreditCardInfoDto?>(null);

        public Task<List<CreditCardInfoDto>> GetActiveByClientAsync(string clientId)
            => Task.FromResult(new List<CreditCardInfoDto>());

        public Task<int> CountActiveAsync() => Task.FromResult(0);

        public Task<decimal> GetDebtByClientAsync(string clientId) => Task.FromResult(0m);

        public Task ApplyPaymentAsync(int creditCardId, decimal effectiveAmount,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "El modulo de tarjetas aun no esta integrado: no se aplico el pago de {Amount} a la tarjeta {CardId}.",
                effectiveAmount, creditCardId);
            return Task.CompletedTask;
        }
    }
}
