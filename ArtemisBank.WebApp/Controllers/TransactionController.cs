using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Beneficiaries;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.WebApp.Common;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    /// <summary>
    /// Operaciones transaccionales del cliente: express, pago a tarjeta, pago a prestamo
    /// y transaccion a beneficiarios.
    /// </summary>
    [Authorize(Roles = ArtemisRoles.Cliente)]
    public class TransactionController : BaseController
    {
        private readonly ITransactionService _transactionService;
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly IBeneficiaryService _beneficiaryService;
        private readonly IUserReadService _userReadService;
        private readonly ILoanReadService _loanReadService;
        private readonly ICreditCardReadService _creditCardReadService;
        private readonly IMapper _mapper;

        public TransactionController(
            ITransactionService transactionService,
            ISavingsAccountService savingsAccountService,
            IBeneficiaryService beneficiaryService,
            IUserReadService userReadService,
            ILoanReadService loanReadService,
            ICreditCardReadService creditCardReadService,
            IMapper mapper,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _transactionService = transactionService;
            _savingsAccountService = savingsAccountService;
            _beneficiaryService = beneficiaryService;
            _userReadService = userReadService;
            _loanReadService = loanReadService;
            _creditCardReadService = creditCardReadService;
            _mapper = mapper;
        }

        // ================================================================== transaccion express

        [HttpGet]
        public async Task<IActionResult> Express(CancellationToken cancellationToken)
            => View(new ExpressTransactionViewModel { SourceAccounts = await LoadAccountsAsync(cancellationToken) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Express(ExpressTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return await BackToExpress(model, cancellationToken);

            var target = await _savingsAccountService.GetEntityByNumberAsync(model.TargetAccountNumber,
                cancellationToken);

            if (target == null || !target.IsActive)
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber), AppMessages.InvalidAccountNumber);
                return await BackToExpress(model, cancellationToken);
            }

            if (string.Equals(target.AccountNumber, model.SourceAccountNumber, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber), AppMessages.SameAccountNotAllowed);
                return await BackToExpress(model, cancellationToken);
            }

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive ||
                !string.Equals(source.ClientId, UserId, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidAccountNumber);
                return await BackToExpress(model, cancellationToken);
            }

            if (source.Balance < (model.Amount ?? 0m))
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsExpress);
                return await BackToExpress(model, cancellationToken);
            }

            var owner = await _userReadService.GetByIdAsync(target.ClientId);

            return View("Confirm", new ConfirmTransactionViewModel
            {
                Mode = "Express",
                SourceAccountNumber = model.SourceAccountNumber,
                TargetAccountNumber = target.AccountNumber,
                TargetOwnerFullName = owner?.FullName ?? string.Empty,
                Amount = model.Amount ?? 0m
            });
        }

        // ================================================================== transaccion a beneficiario

        [HttpGet]
        public async Task<IActionResult> Beneficiaries(CancellationToken cancellationToken)
        {
            var beneficiaries = await _beneficiaryService.GetByClientAsync(UserId, cancellationToken);
            if (beneficiaries.Count == 0) Warning(AppMessages.NoBeneficiariesRegistered);

            return View(new BeneficiaryTransactionViewModel
            {
                Beneficiaries = beneficiaries,
                SourceAccounts = await LoadAccountsAsync(cancellationToken)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Beneficiaries(BeneficiaryTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return await BackToBeneficiaries(model, cancellationToken);

            var beneficiary = await _beneficiaryService.GetByIdAsync(UserId, model.BeneficiaryId ?? 0,
                cancellationToken);

            if (beneficiary == null)
            {
                ModelState.AddModelError(nameof(model.BeneficiaryId), AppMessages.BeneficiaryAccountUnavailable);
                return await BackToBeneficiaries(model, cancellationToken);
            }

            var target = await _savingsAccountService.GetEntityByNumberAsync(beneficiary.AccountNumber,
                cancellationToken);

            if (target == null || !target.IsActive)
            {
                ModelState.AddModelError(nameof(model.BeneficiaryId), AppMessages.BeneficiaryAccountUnavailable);
                return await BackToBeneficiaries(model, cancellationToken);
            }

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive ||
                !string.Equals(source.ClientId, UserId, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidAccountNumber);
                return await BackToBeneficiaries(model, cancellationToken);
            }

            if (source.Balance < (model.Amount ?? 0m))
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsBeneficiary);
                return await BackToBeneficiaries(model, cancellationToken);
            }

            return View("Confirm", new ConfirmTransactionViewModel
            {
                Mode = "Beneficiario",
                BeneficiaryId = beneficiary.Id,
                SourceAccountNumber = model.SourceAccountNumber,
                TargetAccountNumber = beneficiary.AccountNumber,
                TargetOwnerFullName = beneficiary.FullName,
                Amount = model.Amount ?? 0m
            });
        }

        // ================================================================== confirmacion comun

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            var operation = model.Mode == "Beneficiario"
                ? TransactionOperation.TransaccionBeneficiario
                : TransactionOperation.TransaccionExpress;

            var result = await _transactionService.TransferAsync(new TransferRequest
            {
                SourceAccountNumber = model.SourceAccountNumber,
                TargetAccountNumber = model.TargetAccountNumber,
                Amount = model.Amount,
                Operation = operation,
                Actor = ActorAs(Roles.Cliente),
                ExpectedSourceOwnerId = UserId
            }, cancellationToken);

            if (!result.Succeeded)
            {
                Error(result.ErrorMessage!);
                return RedirectToAction(model.Mode == "Beneficiario" ? nameof(Beneficiaries) : nameof(Express));
            }

            Success("Transacción realizada correctamente.");
            Warning(result.Warning);
            return RedirectToAction("Index", "ClientHome");
        }

        // ================================================================== pago a tarjeta de credito

        [HttpGet]
        public async Task<IActionResult> CreditCard(CancellationToken cancellationToken)
            => View(new ClientCardPaymentViewModel
            {
                CreditCards = _mapper.Map<List<CreditCardOptionViewModel>>(
                    await _creditCardReadService.GetActiveByClientAsync(UserId)),
                SourceAccounts = await LoadAccountsAsync(cancellationToken)
            });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreditCard(ClientCardPaymentViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return await BackToCard(model, cancellationToken);

            var result = await _transactionService.PayCreditCardAsync(new CardPaymentRequest
            {
                SourceAccountNumber = model.SourceAccountNumber,
                CreditCardId = model.CreditCardId ?? 0,
                Amount = model.Amount ?? 0m,
                Actor = ActorAs(Roles.Cliente),
                ExpectedSourceOwnerId = UserId
            }, cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return await BackToCard(model, cancellationToken);
            }

            Success("Pago aplicado correctamente a la tarjeta de crédito.");
            Warning(result.Warning);
            return RedirectToAction("Index", "ClientHome");
        }

        // ================================================================== pago a prestamo

        [HttpGet]
        public async Task<IActionResult> Loan(CancellationToken cancellationToken)
            => View(new ClientLoanPaymentViewModel
            {
                Loans = _mapper.Map<List<LoanOptionViewModel>>(
                    await _loanReadService.GetActiveByClientAsync(UserId)),
                SourceAccounts = await LoadAccountsAsync(cancellationToken)
            });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Loan(ClientLoanPaymentViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return await BackToLoan(model, cancellationToken);

            var result = await _transactionService.PayLoanAsync(new LoanPaymentRequest
            {
                SourceAccountNumber = model.SourceAccountNumber,
                LoanId = model.LoanId ?? 0,
                Amount = model.Amount ?? 0m,
                Actor = ActorAs(Roles.Cliente),
                ExpectedSourceOwnerId = UserId
            }, cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return await BackToLoan(model, cancellationToken);
            }

            Success("Pago aplicado correctamente al préstamo.");
            Warning(result.Warning);
            return RedirectToAction("Index", "ClientHome");
        }

        // ================================================================== helpers

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

        private async Task<IActionResult> BackToExpress(ExpressTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            model.SourceAccounts = await LoadAccountsAsync(cancellationToken);
            return View(nameof(Express), model);
        }

        private async Task<IActionResult> BackToBeneficiaries(BeneficiaryTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            model.Beneficiaries = await _beneficiaryService.GetByClientAsync(UserId, cancellationToken);
            model.SourceAccounts = await LoadAccountsAsync(cancellationToken);
            return View(nameof(Beneficiaries), model);
        }

        private async Task<IActionResult> BackToCard(ClientCardPaymentViewModel model,
            CancellationToken cancellationToken)
        {
            model.CreditCards = _mapper.Map<List<CreditCardOptionViewModel>>(
                await _creditCardReadService.GetActiveByClientAsync(UserId));
            model.SourceAccounts = await LoadAccountsAsync(cancellationToken);
            return View(nameof(CreditCard), model);
        }

        private async Task<IActionResult> BackToLoan(ClientLoanPaymentViewModel model,
            CancellationToken cancellationToken)
        {
            model.Loans = _mapper.Map<List<LoanOptionViewModel>>(
                await _loanReadService.GetActiveByClientAsync(UserId));
            model.SourceAccounts = await LoadAccountsAsync(cancellationToken);
            return View(nameof(Loan), model);
        }
    }
}
