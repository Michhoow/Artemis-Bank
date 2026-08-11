using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Client;
using ArtemisBank.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using ArtemisBank.WebApp.Common;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    /// <summary>Home del cliente: productos financieros activos y detalle de cada uno.</summary>
    [Authorize(Roles = ArtemisRoles.Cliente)]
    public class ClientHomeController : BaseController
    {
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly ILoanReadService _loanReadService;
        private readonly ICreditCardReadService _creditCardReadService;
        private readonly IMapper _mapper;

        public ClientHomeController(
            ISavingsAccountService savingsAccountService,
            ILoanReadService loanReadService,
            ICreditCardReadService creditCardReadService,
            IMapper mapper,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _savingsAccountService = savingsAccountService;
            _loanReadService = loanReadService;
            _creditCardReadService = creditCardReadService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            // GetActiveByClientAsync ya devuelve la principal primero y las secundarias
            // ordenadas de mayor a menor balance.
            var accounts = await _savingsAccountService.GetActiveByClientAsync(UserId, cancellationToken);
            var loans = await _loanReadService.GetActiveByClientAsync(UserId);
            var cards = await _creditCardReadService.GetActiveByClientAsync(UserId);

            return View(new ClientHomeViewModel
            {
                SavingsAccounts = _mapper.Map<List<SavingsAccountViewModel>>(accounts),
                Loans = _mapper.Map<List<ClientLoanViewModel>>(loans),
                CreditCards = _mapper.Map<List<ClientCreditCardViewModel>>(cards)
            });
        }

        /// <summary>Historial de una cuenta. El cliente solo ve cuentas que le pertenecen.</summary>
        public async Task<IActionResult> AccountDetails(string accountNumber, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var account = await _savingsAccountService.GetEntityByNumberAsync(accountNumber, cancellationToken);

            if (account == null || !string.Equals(account.ClientId, UserId, StringComparison.Ordinal))
                return RedirectToAction("AccessDenied", "Login");

            var details = await _savingsAccountService.GetTransactionsAsync(accountNumber, page,
                PagedResult<TransactionViewModel>.MaxPageSize, cancellationToken);

            return View(new AccountTransactionsViewModel
            {
                AccountNumber = details.AccountNumber,
                ClientFullName = details.ClientFullName,
                Balance = details.Balance,
                Type = details.Type,
                Status = details.Status,
                Transactions = PagedResult<TransactionViewModel>.Create(
                    _mapper.Map<List<TransactionViewModel>>(details.Transactions.Data),
                    details.Transactions.Page, details.Transactions.PageSize, details.Transactions.TotalRecords)
            });
        }
    }
}
