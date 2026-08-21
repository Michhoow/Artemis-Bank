using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ILoanRepository : IGenericRepository<Loan>
    {
        Task<Loan?> GetWithInstallmentsAsync(int loanId, CancellationToken cancellationToken = default);

        Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);

        Task<Loan?> GetActiveByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<List<Loan>> GetAllByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default);

        Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        Task<decimal> GetPendingDebtByClientAsync(string clientId, CancellationToken cancellationToken = default);

        IQueryable<Loan> QueryWithInstallments();
    }
}
