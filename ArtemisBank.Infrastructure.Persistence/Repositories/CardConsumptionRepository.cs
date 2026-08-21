using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class CardConsumptionRepository : GenericRepository<CardConsumption>, ICardConsumptionRepository
    {
        public CardConsumptionRepository(ArtemisDbContext context) : base(context) { }

        public async Task<List<CardConsumption>> GetByCardAsync(int creditCardId,
            CancellationToken cancellationToken = default)
            => await GetByCardQuery(creditCardId).ToListAsync(cancellationToken);

        public IQueryable<CardConsumption> GetByCardQuery(int creditCardId)
            => Context.CardConsumptions
                .Where(k => k.CreditCardId == creditCardId)
                .OrderByDescending(k => k.CreatedAt)
                .ThenByDescending(k => k.Id);
    }
}
