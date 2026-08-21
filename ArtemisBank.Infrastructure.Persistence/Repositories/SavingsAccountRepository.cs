using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class SavingsAccountRepository : GenericRepository<SavingsAccount>, ISavingsAccountRepository
    {
        public SavingsAccountRepository(ArtemisDbContext context) : base(context) { }

        public async Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;
            var normalized = accountNumber.Trim();
            return await Context.SavingsAccounts.FirstOrDefaultAsync(a => a.AccountNumber == normalized);
        }

        public async Task<SavingsAccount?> GetPrincipalByClientAsync(string clientId)
            => await Context.SavingsAccounts
                .FirstOrDefaultAsync(a => a.ClientId == clientId && a.Type == AccountType.Principal);

        public async Task<List<SavingsAccount>> GetActiveByClientAsync(string clientId)
            => await Context.SavingsAccounts
                .Where(a => a.ClientId == clientId && a.Status == AccountStatus.Activa)
                .ToListAsync();

        public async Task<bool> AccountNumberExistsAsync(string accountNumber)
            => await Context.SavingsAccounts.AnyAsync(a => a.AccountNumber == accountNumber);

        public async Task<int> CountActiveAsync()
            => await Context.SavingsAccounts.CountAsync(a => a.Status == AccountStatus.Activa);

        public async Task<decimal> GetTotalBalanceByClientAsync(string clientId)
            => await Context.SavingsAccounts
                .Where(a => a.ClientId == clientId && a.Status == AccountStatus.Activa)
                .SumAsync(a => (decimal?)a.Balance) ?? 0m;
    }
}
