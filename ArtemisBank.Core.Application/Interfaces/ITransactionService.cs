using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Transactions;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<OperationResult> TransferAsync(TransferRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> DepositAsync(DepositRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> WithdrawAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> PayCreditCardAsync(CardPaymentRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> PayLoanAsync(LoanPaymentRequest request, CancellationToken cancellationToken = default);

        Task<OperationResult> RegisterExternalCreditAsync(ExternalCreditRequest request,
            CancellationToken cancellationToken = default);

        Task<OperationResult> RegisterExternalDebitAsync(ExternalDebitRequest request,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TransactionDto>> GetByAccountAsync(int savingsAccountId, int page, int pageSize,
            CancellationToken cancellationToken = default);
    }
}
