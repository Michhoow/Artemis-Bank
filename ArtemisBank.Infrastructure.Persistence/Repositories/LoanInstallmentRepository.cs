using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class LoanInstallmentRepository : GenericRepository<LoanInstallment>, ILoanInstallmentRepository
    {
        public LoanInstallmentRepository(ArtemisDbContext context) : base(context) { }

        public async Task<List<LoanInstallment>> GetByLoanAsync(int loanId,
            CancellationToken cancellationToken = default)
            => await Context.LoanInstallments
                .Where(i => i.LoanId == loanId)
                .OrderBy(i => i.Number)
                .ToListAsync(cancellationToken);

        public async Task<List<LoanInstallment>> GetPendingByLoanAsync(int loanId,
            CancellationToken cancellationToken = default)
            => await Context.LoanInstallments
                .Where(i => i.LoanId == loanId && i.PaidAmount < i.TotalAmount)
                .OrderBy(i => i.Number)
                .ToListAsync(cancellationToken);

        public async Task<List<LoanInstallment>> GetOverdueAsync(DateTime asOf,
            CancellationToken cancellationToken = default)
            => await Context.LoanInstallments
                .Include(i => i.Loan)
                .Where(i => i.DueDate < asOf
                            && i.PaidAmount < i.TotalAmount
                            && !i.IsOverdue
                            && i.Loan!.Status == LoanStatus.Activo)
                .OrderBy(i => i.DueDate)
                .ToListAsync(cancellationToken);
    }
}
