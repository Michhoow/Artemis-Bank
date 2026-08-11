using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Transactions;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Nucleo de movimiento de dinero. Toda operacion es atomica:
    /// o se aplican todas las patas o no se aplica ninguna.
    /// Dueno: Michael.
    /// </summary>
    public interface ITransactionService
    {
        Task<OperationResult> TransferAsync(TransferRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> DepositAsync(DepositRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> WithdrawAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> PayCreditCardAsync(CardPaymentRequest request, CancellationToken cancellationToken = default);
        Task<OperationResult> PayLoanAsync(LoanPaymentRequest request, CancellationToken cancellationToken = default);

        /// <summary>Credito externo hacia una cuenta (desembolso de prestamo, avance, Hermes Pay).</summary>
        Task<OperationResult> RegisterExternalCreditAsync(ExternalCreditRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>Debito externo desde una cuenta con trazabilidad completa.</summary>
        Task<OperationResult> RegisterExternalDebitAsync(ExternalDebitRequest request,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TransactionDto>> GetByAccountAsync(int savingsAccountId, int page, int pageSize,
            CancellationToken cancellationToken = default);
    }
}
