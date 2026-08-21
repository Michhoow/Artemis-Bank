using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    public interface ILoanReadService
    {
        Task<LoanInfoDto?> GetByIdAsync(int loanId);
        Task<LoanInfoDto?> GetByLoanNumberAsync(string loanNumber);
        Task<List<LoanInfoDto>> GetActiveByClientAsync(string clientId);
        Task<int> CountActiveAsync();
        Task<decimal> GetPendingDebtByClientAsync(string clientId);

        Task<bool> LoanNumberExistsAsync(string number);

        Task ApplyPaymentAsync(int loanId, decimal effectiveAmount, CancellationToken cancellationToken = default);
    }
}
