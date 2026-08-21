using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ICardConsumptionRepository : IGenericRepository<CardConsumption>
    {
        Task<List<CardConsumption>> GetByCardAsync(int creditCardId, CancellationToken cancellationToken = default);

        IQueryable<CardConsumption> GetByCardQuery(int creditCardId);
    }
}
