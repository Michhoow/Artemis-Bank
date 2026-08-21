using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.CreditCards;
using ArtemisBank.Core.Application.ViewModels.Loans;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class CreditCardController : BaseController
    {
        private readonly ICreditCardService _cardService;
        private readonly IUserReadService _userReadService;

        public CreditCardController(
            ICreditCardService cardService,
            IUserReadService userReadService,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _cardService = cardService;
            _userReadService = userReadService;
        }

        public async Task<IActionResult> Index(string? identification, string? status, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "activa" : status;

            var result = await _cardService.GetPagedAsync(new CreditCardFilterDto
            {
                Page = page,
                PageSize = 20,
                Identification = identification,
                Status = effectiveStatus
            }, cancellationToken);

            string? info = null;
            if (result.TotalRecords == 0 && !string.IsNullOrWhiteSpace(identification))
            {
                var client = await _userReadService.GetByIdentificationAsync(identification.Trim());
                info = client == null
                    ? AppMessages.ClientNotFoundByIdentification
                    : "Este cliente no tiene tarjetas de crédito con el filtro seleccionado.";
            }

            return View(new CreditCardIndexViewModel
            {
                Cards = result,
                Identification = identification,
                Status = effectiveStatus,
                InfoMessage = info
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectClient(string? identification)
            => View(new SelectCardClientViewModel
            {
                Identification = identification,
                Clients = await LoadClientsAsync(identification)
            });

        [HttpGet]
        public async Task<IActionResult> Assign(string clientId)
        {
            var client = await _userReadService.GetByIdAsync(clientId);
            if (client == null)
            {
                Error(AppMessages.LoanClientNotFound);
                return RedirectToAction(nameof(SelectClient));
            }

            if (!client.IsActive)
            {
                Error(AppMessages.CardOnlyActiveClients);
                return RedirectToAction(nameof(SelectClient));
            }

            return View(new AssignCreditCardViewModel
            {
                ClientId = client.Id,
                ClientDisplay = $"{client.FullName} — {client.Identification}"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignCreditCardViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var created = await _cardService.CreateAsync(new CreateCreditCardDto
                {
                    ClientId = model.ClientId,
                    CreditLimit = model.CreditLimit
                }, UserId, cancellationToken);

                Success(AppMessages.CardCreated);

                return View("Created", new CreatedCardViewModel
                {
                    Card = created.Card,
                    Cvc = created.Cvc
                });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardService.GetDetailAsync(id, cancellationToken);
            if (card == null)
            {
                Error(AppMessages.CardNotFound);
                return RedirectToAction(nameof(Index));
            }

            return View(card);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardService.GetDetailAsync(id, cancellationToken);
            if (card == null)
            {
                Error(AppMessages.CardNotFound);
                return RedirectToAction(nameof(Index));
            }

            if (!card.IsActive)
            {
                Error(AppMessages.CardNotActive);
                return RedirectToAction(nameof(Index));
            }

            return View(new EditCardLimitViewModel
            {
                CreditCardId = card.Id,
                MaskedNumber = card.MaskedNumber,
                ClientFullName = card.ClientFullName,
                CurrentDebt = card.Debt,
                CreditLimit = card.CreditLimit
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCardLimitViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _cardService.UpdateLimitAsync(model.CreditCardId, model.CreditLimit, UserId,
                    cancellationToken);

                Success(AppMessages.CardLimitUpdated);
                return RedirectToAction(nameof(Details), new { id = model.CreditCardId });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardService.GetDetailAsync(id, cancellationToken);
            if (card == null)
            {
                Error(AppMessages.CardNotFound);
                return RedirectToAction(nameof(Index));
            }

            return View(new CancelCardViewModel
            {
                CreditCardId = card.Id,
                MaskedNumber = card.MaskedNumber,
                ClientFullName = card.ClientFullName,
                Debt = card.Debt
            });
        }

        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int creditCardId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _cardService.CancelAsync(creditCardId, UserId, cancellationToken);
                Success(AppMessages.CardCancelled);
            }
            catch (BusinessRuleException ex)
            {
                Error(ex.Message);
            }
            catch (NotFoundException ex)
            {
                Error(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<ClientOptionViewModel>> LoadClientsAsync(string? identification)
        {
            var clients = await _userReadService.GetClientsAsync(onlyActive: true);

            if (!string.IsNullOrWhiteSpace(identification))
            {
                var needle = identification.Trim();
                clients = clients.Where(c => c.Identification.Contains(needle)).ToList();
            }

            return clients.Select(c => new ClientOptionViewModel
            {
                Id = c.Id,
                FullName = c.FullName,
                Identification = c.Identification,
                Email = c.Email,
                HasPrincipalAccount = true
            }).ToList();
        }
    }
}
