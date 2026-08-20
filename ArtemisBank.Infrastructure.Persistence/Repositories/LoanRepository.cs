using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class LoanRepository : GenericRepository<Loan>, ILoanRepository
    {
        public LoanRepository(ArtemisDbContext context) : base(context) { }

        public async Task<Loan?> GetByIdWithInstallmentsAsync(int loanId, CancellationToken cancellationToken = default)
            => await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

        public async Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(loanNumber)) return null;
            var normalized = loanNumber.Trim();
            return await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.LoanNumber == normalized, cancellationToken);
        }

        public async Task<Loan?> GetActiveByClientAsync(string clientId, CancellationToken cancellationToken = default)
            => await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.ClientId == clientId && l.Status == LoanStatus.Activo, cancellationToken);

        public async Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default)
            => await Context.Loans.AnyAsync(l => l.ClientId == clientId && l.Status == LoanStatus.Activo,
                cancellationToken);

        public async Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default)
            => await Context.Loans.AnyAsync(l => l.LoanNumber == loanNumber, cancellationToken);

        public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => await Context.Loans.CountAsync(l => l.Status == LoanStatus.Activo, cancellationToken);

        public async Task<decimal> GetActivePendingDebtByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.Installments
                .Where(i => i.Loan!.ClientId == clientId && i.Loan.Status == LoanStatus.Activo)
                .SumAsync(i => (decimal?)i.PendingAmount, cancellationToken) ?? 0m;

        public async Task<List<Installment>> GetOverdueCandidateInstallmentsAsync(DateTime asOf,
            CancellationToken cancellationToken = default)
            => await Context.Installments
                .Include(i => i.Loan)
                .Where(i => i.Loan!.Status == LoanStatus.Activo
                            && i.Status != InstallmentStatus.Pagada
                            && i.DueDate < asOf)
                .ToListAsync(cancellationToken);

        public IQueryable<Loan> QueryWithInstallments()
            => Context.Loans.Include(l => l.Installments).AsQueryable();
    }
}
