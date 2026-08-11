using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Cashier;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    /// <summary>
    /// Modulo de ventanilla. Toda operacion pasa por una pantalla de confirmacion previa
    /// y queda asociada al cajero autenticado que la realizo.
    /// </summary>
    [Authorize(Roles = ArtemisRoles.Cajero)]
    public class CashierController : BaseController
    {
        private readonly ICashierHomeService _cashierHomeService;
        private readonly ITransactionService _transactionService;
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly IUserReadService _userReadService;
        private readonly ICreditCardReadService _creditCardReadService;
        private readonly ILoanReadService _loanReadService;

        public CashierController(
            ICashierHomeService cashierHomeService,
            ITransactionService transactionService,
            ISavingsAccountService savingsAccountService,
            IUserReadService userReadService,
            ICreditCardReadService creditCardReadService,
            ILoanReadService loanReadService,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _cashierHomeService = cashierHomeService;
            _transactionService = transactionService;
            _savingsAccountService = savingsAccountService;
            _userReadService = userReadService;
            _creditCardReadService = creditCardReadService;
            _loanReadService = loanReadService;
        }

        // ================================================================== home

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
            => View(await _cashierHomeService.GetIndicatorsAsync(UserId, cancellationToken));

        // ================================================================== deposito

        [HttpGet]
        public IActionResult Deposit() => View(new DepositViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            var target = await _savingsAccountService.GetEntityByNumberAsync(model.TargetAccountNumber,
                cancellationToken);

            if (target == null || !target.IsActive)
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber), AppMessages.InvalidAccountNumber);
                return View(model);
            }

            var owner = await _userReadService.GetByIdAsync(target.ClientId);

            return View("Confirm", new CashierConfirmViewModel
            {
                Operation = "Deposito",
                Title = "Confirmar depósito",
                ConfirmationMessage = AppMessages.ConfirmDeposit,
                TargetAccountNumber = target.AccountNumber,
                TargetOwnerFullName = owner?.FullName ?? string.Empty,
                EnteredAmount = model.Amount ?? 0m,
                EffectiveAmount = model.Amount ?? 0m
            });
        }

        // ================================================================== retiro

        [HttpGet]
        public IActionResult Withdrawal() => View(new WithdrawalViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdrawal(WithdrawalViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive)
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidAccountNumber);
                return View(model);
            }

            if (source.Balance < (model.Amount ?? 0m))
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsAccount);
                return View(model);
            }

            var owner = await _userReadService.GetByIdAsync(source.ClientId);

            return View("Confirm", new CashierConfirmViewModel
            {
                Operation = "Retiro",
                Title = "Confirmar retiro",
                ConfirmationMessage = AppMessages.ConfirmWithdrawal,
                SourceAccountNumber = source.AccountNumber,
                SourceOwnerFullName = owner?.FullName ?? string.Empty,
                EnteredAmount = model.Amount ?? 0m,
                EffectiveAmount = model.Amount ?? 0m
            });
        }

        // ================================================================== pago a tarjeta

        [HttpGet]
        public IActionResult CardPayment() => View(new CashierCardPaymentViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CardPayment(CashierCardPaymentViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive)
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidAccountNumber);
                return View(model);
            }

            var card = await _creditCardReadService.GetByCardNumberAsync(model.CardNumber);

            if (card == null || !card.IsActive)
            {
                ModelState.AddModelError(nameof(model.CardNumber), AppMessages.InvalidCardNumber);
                return View(model);
            }

            if (!card.HasDebt)
            {
                ModelState.AddModelError(nameof(model.CardNumber), AppMessages.CardWithoutDebt);
                return View(model);
            }

            // Regla anti sobrepago: el monto efectivo nunca supera la deuda real.
            var effective = Money.Round(Math.Min(model.Amount ?? 0m, card.Debt));

            if (source.Balance < effective)
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsAccount);
                return View(model);
            }

            var accountOwner = await _userReadService.GetByIdAsync(source.ClientId);
            var cardOwner = await _userReadService.GetByIdAsync(card.ClientId);

            return View("Confirm", new CashierConfirmViewModel
            {
                Operation = "PagoTarjeta",
                Title = "Confirmar pago a tarjeta de crédito",
                ConfirmationMessage = AppMessages.ConfirmPayment,
                SourceAccountNumber = source.AccountNumber,
                SourceOwnerFullName = accountOwner?.FullName ?? string.Empty,
                CardLastFour = card.LastFourDigits,
                CardOwnerFullName = cardOwner?.FullName ?? string.Empty,
                CreditCardId = card.Id,
                EnteredAmount = model.Amount ?? 0m,
                EffectiveAmount = effective,
                ShowEffectiveAmount = true
            });
        }

        // ================================================================== pago a prestamo

        [HttpGet]
        public IActionResult LoanPayment() => View(new CashierLoanPaymentViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoanPayment(CashierLoanPaymentViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive)
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidAccountNumber);
                return View(model);
            }

            var loan = await _loanReadService.GetByLoanNumberAsync(model.LoanNumber);

            if (loan == null || !loan.IsActive)
            {
                ModelState.AddModelError(nameof(model.LoanNumber), AppMessages.InvalidLoanNumber);
                return View(model);
            }

            if (!loan.HasPendingInstallments)
            {
                ModelState.AddModelError(nameof(model.LoanNumber), AppMessages.LoanWithoutPendingInstallments);
                return View(model);
            }

            var effective = Money.Round(Math.Min(model.Amount ?? 0m, loan.PendingAmount));

            if (source.Balance < effective)
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsAccount);
                return View(model);
            }

            var accountOwner = await _userReadService.GetByIdAsync(source.ClientId);
            var loanOwner = await _userReadService.GetByIdAsync(loan.ClientId);

            return View("Confirm", new CashierConfirmViewModel
            {
                Operation = "PagoPrestamo",
                Title = "Confirmar pago a préstamo",
                ConfirmationMessage = AppMessages.ConfirmPayment,
                SourceAccountNumber = source.AccountNumber,
                SourceOwnerFullName = accountOwner?.FullName ?? string.Empty,
                LoanNumber = loan.LoanNumber,
                LoanOwnerFullName = loanOwner?.FullName ?? string.Empty,
                LoanId = loan.Id,
                EnteredAmount = model.Amount ?? 0m,
                EffectiveAmount = effective,
                ShowEffectiveAmount = true
            });
        }

        // ================================================================== transacciones a terceros

        [HttpGet]
        public IActionResult ThirdParty() => View(new ThirdPartyTransactionViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThirdParty(ThirdPartyTransactionViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.Equals(model.SourceAccountNumber, model.TargetAccountNumber, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber), AppMessages.SameAccountCashier);
                return View(model);
            }

            var source = await _savingsAccountService.GetEntityByNumberAsync(model.SourceAccountNumber,
                cancellationToken);

            if (source == null || !source.IsActive)
            {
                ModelState.AddModelError(nameof(model.SourceAccountNumber), AppMessages.InvalidOriginAccount);
                return View(model);
            }

            var target = await _savingsAccountService.GetEntityByNumberAsync(model.TargetAccountNumber,
                cancellationToken);

            if (target == null || !target.IsActive)
            {
                ModelState.AddModelError(nameof(model.TargetAccountNumber), AppMessages.InvalidTargetAccount);
                return View(model);
            }

            if (source.Balance < (model.Amount ?? 0m))
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.InsufficientFundsAccount);
                return View(model);
            }

            var sourceOwner = await _userReadService.GetByIdAsync(source.ClientId);
            var targetOwner = await _userReadService.GetByIdAsync(target.ClientId);

            return View("Confirm", new CashierConfirmViewModel
            {
                Operation = "Terceros",
                Title = "Confirmar transacción a cuenta de terceros",
                ConfirmationMessage = AppMessages.ConfirmTransaction,
                SourceAccountNumber = source.AccountNumber,
                SourceOwnerFullName = sourceOwner?.FullName ?? string.Empty,
                TargetAccountNumber = target.AccountNumber,
                TargetOwnerFullName = targetOwner?.FullName ?? string.Empty,
                EnteredAmount = model.Amount ?? 0m,
                EffectiveAmount = model.Amount ?? 0m
            });
        }

        // ================================================================== confirmacion unica

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(CashierConfirmViewModel model, CancellationToken cancellationToken)
        {
            var actor = ActorAs(Roles.Cajero);

            OperationResult result = model.Operation switch
            {
                "Deposito" => await _transactionService.DepositAsync(new DepositRequest
                {
                    TargetAccountNumber = model.TargetAccountNumber ?? string.Empty,
                    Amount = model.EffectiveAmount,
                    Actor = actor
                }, cancellationToken),

                "Retiro" => await _transactionService.WithdrawAsync(new WithdrawalRequest
                {
                    SourceAccountNumber = model.SourceAccountNumber ?? string.Empty,
                    Amount = model.EffectiveAmount,
                    Actor = actor
                }, cancellationToken),

                "PagoTarjeta" => await _transactionService.PayCreditCardAsync(new CardPaymentRequest
                {
                    SourceAccountNumber = model.SourceAccountNumber ?? string.Empty,
                    CreditCardId = model.CreditCardId ?? 0,
                    Amount = model.EnteredAmount,
                    Actor = actor
                }, cancellationToken),

                "PagoPrestamo" => await _transactionService.PayLoanAsync(new LoanPaymentRequest
                {
                    SourceAccountNumber = model.SourceAccountNumber ?? string.Empty,
                    LoanId = model.LoanId ?? 0,
                    Amount = model.EnteredAmount,
                    Actor = actor
                }, cancellationToken),

                "Terceros" => await _transactionService.TransferAsync(new TransferRequest
                {
                    SourceAccountNumber = model.SourceAccountNumber ?? string.Empty,
                    TargetAccountNumber = model.TargetAccountNumber ?? string.Empty,
                    Amount = model.EffectiveAmount,
                    Operation = TransactionOperation.TransaccionTerceros,
                    Actor = actor
                }, cancellationToken),

                _ => OperationResult.Failure("Operación no reconocida.")
            };

            if (result.Succeeded)
            {
                Success("Operación realizada correctamente.");
                Warning(result.Warning);
            }
            else
            {
                Error(result.ErrorMessage!);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
