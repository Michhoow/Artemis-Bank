using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.ViewModels.Home;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Indicadores del Home del cajero: solo operaciones del cajero autenticado y solo del dia actual.
    /// </summary>
    public class CashierHomeService : ICashierHomeService
    {
        private readonly ITransactionRepository _transactionRepository;

        public CashierHomeService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<CashierHomeViewModel> GetIndicatorsAsync(string cashierUserId,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.Now.Date;

            return new CashierHomeViewModel
            {
                TodayTransactions = await _transactionRepository.CountByCashierAsync(cashierUserId, today),
                TodayPayments = await _transactionRepository.CountPaymentsByCashierAsync(cashierUserId, today),
                TodayDeposits = await _transactionRepository.CountByCashierAndOperationAsync(
                    cashierUserId, TransactionOperation.Deposito, today),
                TodayWithdrawals = await _transactionRepository.CountByCashierAndOperationAsync(
                    cashierUserId, TransactionOperation.Retiro, today)
            };
        }
    }
}
