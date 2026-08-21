using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Application.ViewModels.CreditCards;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Cliente)]
    public class CashAdvanceController : BaseController
    {
        private readonly ICreditCardService _cardService;
        private readonly ISavingsAccountService _accountService;

        public CashAdvanceController(
            ICreditCardService cardService,
            ISavingsAccountService accountService,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _cardService = cardService;
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var model = await BuildFormAsync(new CashAdvanceViewModel(), cancellationToken);

            if (model.Cards.Count == 0)
                Warning(AppMessages.AdvanceNoActiveCards);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CashAdvanceViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(await BuildFormAsync(model, cancellationToken));

            var card = (await _cardService.GetByClientAsync(UserId, onlyActive: true, cancellationToken))
                .FirstOrDefault(c => c.Id == model.CreditCardId);

            if (card == null)
            {
                ModelState.AddModelError(nameof(model.CreditCardId), AppMessages.CardNotFound);
                return View(await BuildFormAsync(model, cancellationToken));
            }

            var amount = Money.Round(model.Amount);
            var interest = Money.Round(amount * CreditCardService.CashAdvanceInterestRate);
            var total = Money.Round(amount + interest);

            if (total > card.AvailableCredit)
            {
                ModelState.AddModelError(nameof(model.Amount), AppMessages.AdvanceInsufficientCredit);
                return View(await BuildFormAsync(model, cancellationToken));
            }

            return View("Confirm", new ConfirmCashAdvanceViewModel
            {
                CreditCardId = card.Id,
                CardMaskedNumber = card.MaskedNumber,
                TargetAccountNumber = model.TargetAccountNumber,
                Amount = amount,
                InterestAmount = interest,
                TotalCharged = total
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmCashAdvanceViewModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _cardService.CashAdvanceAsync(new CashAdvanceRequestDto
                {
                    CreditCardId = model.CreditCardId,
                    TargetAccountNumber = model.TargetAccountNumber,
                    Amount = model.Amount
                }, UserId, cancellationToken);

                Success($"Avance de efectivo realizado correctamente. " +
                        $"Se acreditaron {Money.Format(result.Amount)} a su cuenta terminada en " +
                        $"{Money.LastFour(result.TargetAccountNumber)} y se cargaron " +
                        $"{Money.Format(result.TotalCharged)} a su tarjeta.");

                return RedirectToAction("Index", "ClientHome");
            }
            catch (BusinessRuleException ex)
            {
                Error(ex.Message);
            }
            catch (ForbiddenException)
            {
                Error(AppMessages.CardNotFound);
            }
            catch (NotFoundException ex)
            {
                Error(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<CashAdvanceViewModel> BuildFormAsync(CashAdvanceViewModel model,
            CancellationToken cancellationToken)
        {
            model.Cards = await _cardService.GetByClientAsync(UserId, onlyActive: true, cancellationToken);
            model.Accounts = await _accountService.GetActiveByClientAsync(UserId, cancellationToken);
            return model;
        }
    }
}
