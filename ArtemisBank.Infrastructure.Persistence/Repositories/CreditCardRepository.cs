using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class CreditCardRepository : GenericRepository<CreditCard>, ICreditCardRepository
    {
        public CreditCardRepository(ArtemisDbContext context) : base(context) { }

        public async Task<CreditCard?> GetWithConsumptionsAsync(int creditCardId,
            CancellationToken cancellationToken = default)
        {
            var card = await Context.CreditCards
                .Include(c => c.Consumptions)
                .FirstOrDefaultAsync(c => c.Id == creditCardId, cancellationToken);

            if (card?.Consumptions != null)
                card.Consumptions = card.Consumptions.OrderByDescending(k => k.CreatedAt).ToList();

            return card;
        }

        public async Task<CreditCard?> GetByCardNumberAsync(string cardNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return null;
            var normalized = cardNumber.Trim();
            return await Context.CreditCards
                .FirstOrDefaultAsync(c => c.CardNumber == normalized, cancellationToken);
        }

        public async Task<List<CreditCard>> GetActiveByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.CreditCards
                .Where(c => c.ClientId == clientId && c.Status == CardStatus.Activa)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<List<CreditCard>> GetAllByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.CreditCards
                .Where(c => c.ClientId == clientId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<bool> CardNumberExistsAsync(string cardNumber,
            CancellationToken cancellationToken = default)
            => await Context.CreditCards.AnyAsync(c => c.CardNumber == cardNumber, cancellationToken);

        public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => await Context.CreditCards.CountAsync(c => c.Status == CardStatus.Activa, cancellationToken);

        public async Task<decimal> GetDebtByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.CreditCards
                .Where(c => c.ClientId == clientId && c.Status == CardStatus.Activa)
                .SumAsync(c => (decimal?)c.Debt, cancellationToken) ?? 0m;

        public IQueryable<CreditCard> Query() => Context.CreditCards.AsQueryable();
    }
}
