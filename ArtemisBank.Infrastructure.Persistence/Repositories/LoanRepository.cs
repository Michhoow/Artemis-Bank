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

        public async Task<Loan?> GetWithInstallmentsAsync(int loanId,
            CancellationToken cancellationToken = default)
        {
            var loan = await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

            OrderInstallments(loan);
            return loan;
        }

        public async Task<Loan?> GetByLoanNumberAsync(string loanNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(loanNumber)) return null;
            var normalized = loanNumber.Trim();

            var loan = await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.LoanNumber == normalized, cancellationToken);

            OrderInstallments(loan);
            return loan;
        }

        public async Task<Loan?> GetActiveByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
        {
            var loan = await Context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.ClientId == clientId && l.Status == LoanStatus.Activo,
                    cancellationToken);

            OrderInstallments(loan);
            return loan;
        }

        public async Task<List<Loan>> GetAllByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.Loans
                .Include(l => l.Installments)
                .Where(l => l.ClientId == clientId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<bool> LoanNumberExistsAsync(string loanNumber,
            CancellationToken cancellationToken = default)
            => await Context.Loans.AnyAsync(l => l.LoanNumber == loanNumber, cancellationToken);

        public async Task<bool> HasActiveLoanAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.Loans.AnyAsync(
                l => l.ClientId == clientId && l.Status == LoanStatus.Activo, cancellationToken);

        public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => await Context.Loans.CountAsync(l => l.Status == LoanStatus.Activo, cancellationToken);

        public async Task<decimal> GetPendingDebtByClientAsync(string clientId,
            CancellationToken cancellationToken = default)
            => await Context.LoanInstallments
                .Where(i => i.Loan!.ClientId == clientId && i.Loan.Status == LoanStatus.Activo)
                .SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount), cancellationToken) ?? 0m;

        public IQueryable<Loan> QueryWithInstallments()
            => Context.Loans.Include(l => l.Installments).AsQueryable();

        private static void OrderInstallments(Loan? loan)
        {
            if (loan?.Installments == null) return;
            loan.Installments = loan.Installments.OrderBy(i => i.Number).ToList();
        }
    }
}
