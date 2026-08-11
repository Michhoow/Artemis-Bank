using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Home;
using ArtemisBank.Core.Domain.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Indicadores generales del Home del administrador.
    /// Las transacciones se cuentan por OPERACION (OperationReference), no por fila:
    /// una transferencia genera dos filas y representa una sola transaccion.
    /// </summary>
    public class AdminHomeService : IAdminHomeService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ISavingsAccountRepository _accountRepository;
        private readonly IUserReadService _userReadService;
        private readonly ILoanReadService _loanReadService;
        private readonly ICreditCardReadService _creditCardReadService;

        public AdminHomeService(
            ITransactionRepository transactionRepository,
            ISavingsAccountRepository accountRepository,
            IUserReadService userReadService,
            ILoanReadService loanReadService,
            ICreditCardReadService creditCardReadService)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _userReadService = userReadService;
            _loanReadService = loanReadService;
            _creditCardReadService = creditCardReadService;
        }

        public async Task<AdminHomeViewModel> GetIndicatorsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Now.Date;

            var activeAccounts = await _accountRepository.CountActiveAsync();
            var activeLoans = await _loanReadService.CountActiveAsync();
            var activeCards = await _creditCardReadService.CountActiveAsync();

            var activeClientIds = await _userReadService.GetActiveClientIdsAsync();

            decimal totalDebt = 0m;
            foreach (var clientId in activeClientIds)
            {
                totalDebt += await _loanReadService.GetPendingDebtByClientAsync(clientId);
                totalDebt += await _creditCardReadService.GetDebtByClientAsync(clientId);
            }

            var averageDebt = activeClientIds.Count == 0
                ? 0m
                : Money.Round(totalDebt / activeClientIds.Count);

            return new AdminHomeViewModel
            {
                TotalHistoricTransactions = await _transactionRepository.CountDistinctOperationsAsync(),
                TodayTransactions = await _transactionRepository.CountDistinctOperationsAsync(today),
                TotalHistoricPayments = await _transactionRepository.CountDistinctPaymentsAsync(),
                TodayPayments = await _transactionRepository.CountDistinctPaymentsAsync(today),
                ActiveClients = activeClientIds.Count,
                InactiveClients = await _userReadService.CountClientsAsync(false),
                ActiveSavingsAccounts = activeAccounts,
                ActiveLoans = activeLoans,
                ActiveCreditCards = activeCards,
                TotalFinancialProducts = activeAccounts + activeLoans + activeCards,
                AverageDebtPerClient = averageDebt
            };
        }
    }
}
