using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Implementacion real del contrato ICreditCardReadService (reemplaza a PendingCreditCardReadService).
    /// Dueno: Manuel. La consume Michael para el pago a tarjeta (cliente y cajero) y los indicadores.
    /// Nunca expone el numero completo ni el CVC.
    /// </summary>
    public class CreditCardReadService : ICreditCardReadService
    {
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreditCardReadService> _logger;

        public CreditCardReadService(ICreditCardRepository creditCardRepository, IUnitOfWork unitOfWork,
            ILogger<CreditCardReadService> logger)
        {
            _creditCardRepository = creditCardRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreditCardInfoDto?> GetByIdAsync(int creditCardId)
        {
            var card = await _creditCardRepository.GetByIdAsync(creditCardId);
            return card == null ? null : ToInfo(card);
        }

        public async Task<CreditCardInfoDto?> GetByCardNumberAsync(string cardNumber)
        {
            var card = await _creditCardRepository.GetByCardNumberAsync(cardNumber);
            return card == null ? null : ToInfo(card);
        }

        public async Task<List<CreditCardInfoDto>> GetActiveByClientAsync(string clientId)
        {
            var cards = await _creditCardRepository.GetActiveByClientAsync(clientId);
            return cards.Select(ToInfo).ToList();
        }

        public Task<int> CountActiveAsync() => _creditCardRepository.CountActiveAsync();

        public Task<decimal> GetDebtByClientAsync(string clientId)
            => _creditCardRepository.GetActiveDebtByClientAsync(clientId);

        public async Task ApplyPaymentAsync(int creditCardId, decimal effectiveAmount,
            CancellationToken cancellationToken = default)
        {
            var card = await _creditCardRepository.GetByIdAsync(creditCardId);
            if (card == null)
            {
                _logger.LogWarning("Se intento aplicar un pago a una tarjeta inexistente {CardId}.", creditCardId);
                return;
            }

            var applied = Money.Round(Math.Min(Money.Round(effectiveAmount), card.Debt));
            card.Debt = Money.Round(card.Debt - applied);

            // Michael ya abrio la transaccion: aqui solo se persiste el cambio dentro de esa unidad de trabajo.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static CreditCardInfoDto ToInfo(CreditCard card) => new CreditCardInfoDto
        {
            Id = card.Id,
            ClientId = card.ClientId,
            LastFourDigits = card.LastFourDigits,
            CreditLimit = card.CreditLimit,
            Debt = card.Debt,
            AvailableCredit = card.AvailableCredit,
            ExpirationDate = card.ExpirationDate,
            IsActive = card.IsActive
        };
    }
}
