using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class BeneficiaryRepository : GenericRepository<Beneficiary>, IBeneficiaryRepository
    {
        public BeneficiaryRepository(ArtemisDbContext context) : base(context) { }

        public async Task<List<Beneficiary>> GetByClientAsync(string clientId)
            => await Context.Beneficiaries
                .Include(b => b.SavingsAccount)
                .Where(b => b.ClientId == clientId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

        public async Task<Beneficiary?> GetByClientAndAccountAsync(string clientId, int savingsAccountId)
            => await Context.Beneficiaries
                .Include(b => b.SavingsAccount)
                .FirstOrDefaultAsync(b => b.ClientId == clientId && b.SavingsAccountId == savingsAccountId);

        public async Task<bool> ExistsAsync(string clientId, int savingsAccountId)
            => await Context.Beneficiaries
                .AnyAsync(b => b.ClientId == clientId && b.SavingsAccountId == savingsAccountId);
    }
}
