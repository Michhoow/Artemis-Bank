using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ILoanInstallmentRepository : IGenericRepository<LoanInstallment>
    {
        Task<List<LoanInstallment>> GetByLoanAsync(int loanId, CancellationToken cancellationToken = default);

        Task<List<LoanInstallment>> GetPendingByLoanAsync(int loanId, CancellationToken cancellationToken = default);

        Task<List<LoanInstallment>> GetOverdueAsync(DateTime asOf, CancellationToken cancellationToken = default);
    }
}
