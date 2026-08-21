using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Cliente)]
    public class TransferController : BaseController
    {
        private readonly ITransactionService _transactionService;
        private readonly ISavingsAccountService _savingsAccountService;

        public TransferController(ITransactionService transactionService,
            ISavingsAccountService savingsAccountService, IAuthenticatedUser currentUser) : base(currentUser)
        {
            _transactionService = transactionService;
            _savingsAccountService = savingsAccountService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var accounts = await LoadAccountsAsync(cancellationToken);

            if (accounts.Count < 2) Warning(AppMessages.NeedTwoActiveAccounts);

            return View(new TransferViewModel { Accounts = accounts });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TransferViewModel model, CancellationToken cancellationToken)
        {
            var accounts = await LoadAccountsAsync(cancellationToken);
            model.Accounts = accounts;

            if (accounts.Count < 2)
            {
                ModelState.AddModelError(string.Empty, AppMessages.NeedTwoActiveAccounts);
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            if (string.Equals(model.SourceAccountNumber, model.TargetAccountNumber, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber),
                    AppMessages.SameAccountTransferNotAllowed);
                return View(model);
            }

            var source = accounts.FirstOrDefault(a => a.AccountNumber == model.SourceAccountNumber);
            var target = accounts.FirstOrDefault(a => a.AccountNumber == model.TargetAccountNumber);

            if (source == null || target == null)
            {
                ModelState.AddModelError(string.Empty, AppMessages.InvalidAccountNumber);
                return View(model);
            }

            if (source.Balance < (model.Amount ?? 0m))
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsGeneric);
                return View(model);
            }

            return View("Confirm", new ConfirmTransferViewModel
            {
                SourceAccountNumber = model.SourceAccountNumber,
                TargetAccountNumber = model.TargetAccountNumber,
                Amount = model.Amount ?? 0m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmTransferViewModel model, CancellationToken cancellationToken)
        {
            var result = await _transactionService.TransferAsync(new TransferRequest
            {
                SourceAccountNumber = model.SourceAccountNumber,
                TargetAccountNumber = model.TargetAccountNumber,
                Amount = model.Amount,
                Operation = TransactionOperation.TransferenciaEntreCuentas,
                Actor = ActorAs(Roles.Cliente),
                ExpectedSourceOwnerId = UserId
            }, cancellationToken);

            if (!result.Succeeded)
            {
                Error(result.ErrorMessage!);
                return RedirectToAction(nameof(Index));
            }

            Success("Transferencia realizada correctamente.");
            Warning(result.Warning);
            return RedirectToAction("Index", "ClientHome");
        }

        private async Task<List<AccountOptionViewModel>> LoadAccountsAsync(CancellationToken cancellationToken)
        {
            var accounts = await _savingsAccountService.GetActiveByClientAsync(UserId, cancellationToken);
            return accounts.Select(a => new AccountOptionViewModel
            {
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                Type = a.Type
            }).ToList();
        }
    }
}
