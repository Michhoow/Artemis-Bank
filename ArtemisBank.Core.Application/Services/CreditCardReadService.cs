using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class CreditCardReadService : ICreditCardReadService
    {
        private readonly ICreditCardRepository _cardRepository;
        private readonly ICardConsumptionRepository _consumptionRepository;
        private readonly ILogger<CreditCardReadService> _logger;

        public CreditCardReadService(
            ICreditCardRepository cardRepository,
            ICardConsumptionRepository consumptionRepository,
            ILogger<CreditCardReadService> logger)
        {
            _cardRepository = cardRepository;
            _consumptionRepository = consumptionRepository;
            _logger = logger;
        }

        public async Task<CreditCardInfoDto?> GetByIdAsync(int creditCardId)
        {
            var card = await _cardRepository.GetByIdAsync(creditCardId);
            return card == null ? null : Map(card);
        }

        public async Task<CreditCardInfoDto?> GetByCardNumberAsync(string cardNumber)
        {
            var card = await _cardRepository.GetByCardNumberAsync(cardNumber);
            return card == null ? null : Map(card);
        }

        public async Task<List<CreditCardInfoDto>> GetActiveByClientAsync(string clientId)
        {
            var cards = await _cardRepository.GetActiveByClientAsync(clientId);
            return cards.Select(Map).ToList();
        }

        public Task<int> CountActiveAsync() => _cardRepository.CountActiveAsync();

        public Task<decimal> GetDebtByClientAsync(string clientId)
            => _cardRepository.GetDebtByClientAsync(clientId);

        public async Task ApplyPaymentAsync(int creditCardId, decimal effectiveAmount,
            CancellationToken cancellationToken = default)
        {
            var amount = Money.Round(effectiveAmount);
            if (amount <= 0m) return;

            var card = await _cardRepository.GetByIdAsync(creditCardId);
            if (card == null)
            {
                _logger.LogWarning("Se intento aplicar un pago a una tarjeta inexistente. Id {CardId}.", creditCardId);
                return;
            }

            var applied = Money.Round(Math.Min(amount, card.Debt));
            card.Debt = Money.Round(card.Debt - applied);

            await _cardRepository.UpdateEntityAsync(card);

            await _consumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                Amount = applied,
                Type = ConsumptionType.Pago,
                Status = ConsumptionStatus.Aprobado,
                Description = DisplayText.ConsumptionType(ConsumptionType.Pago),
                OperationReference = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now
            });

            _logger.LogInformation(
                "Pago aplicado a la tarjeta ****{Last4}. Monto {Amount}. Deuda resultante {Debt}.",
                card.LastFourDigits, applied, card.Debt);
        }

        private static CreditCardInfoDto Map(CreditCard card) => new CreditCardInfoDto
        {
            Id = card.Id,
            ClientId = card.ClientId,

            LastFourDigits = card.LastFourDigits,
            CreditLimit = card.CreditLimit,
            Debt = card.Debt,
            AvailableCredit = card.AvailableCredit,
            ExpirationDate = card.ExpirationDisplay,
            IsActive = card.IsActive && !card.IsExpired()
        };
    }
}
