using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Loans;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class LoanController : BaseController
    {
        private readonly ILoanService _loanService;
        private readonly IUserReadService _userReadService;
        private readonly ISavingsAccountService _accountService;

        public LoanController(
            ILoanService loanService,
            IUserReadService userReadService,
            ISavingsAccountService accountService,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _loanService = loanService;
            _userReadService = userReadService;
            _accountService = accountService;
        }

        public async Task<IActionResult> Index(string? identification, string? status, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "activo" : status;

            var result = await _loanService.GetPagedAsync(new LoanFilterDto
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
                    : "Este cliente no tiene préstamos registrados con el filtro seleccionado.";
            }

            return View(new LoanIndexViewModel
            {
                Loans = result,
                Identification = identification,
                Status = effectiveStatus,
                InfoMessage = info
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectClient(string? identification,
            CancellationToken cancellationToken = default)
            => View(new SelectLoanClientViewModel
            {
                Identification = identification,
                Clients = await LoadClientsAsync(identification, cancellationToken)
            });

        [HttpGet]
        public async Task<IActionResult> Assign(string clientId, CancellationToken cancellationToken = default)
        {
            var client = await _userReadService.GetByIdAsync(clientId);
            if (client == null)
            {
                Error(AppMessages.LoanClientNotFound);
                return RedirectToAction(nameof(SelectClient));
            }

            if (!client.IsActive)
            {
                Error(AppMessages.LoanOnlyActiveClients);
                return RedirectToAction(nameof(SelectClient));
            }

            return View(new AssignLoanViewModel
            {
                ClientId = client.Id,
                ClientDisplay = $"{client.FullName} — {client.Identification}"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignLoanViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View("Assign", model);

            var request = ToDto(model);

            try
            {
                if (!model.ConfirmHighRisk)
                {
                    var evaluation = await _loanService.EvaluateRiskAsync(request, cancellationToken);

                    if (evaluation.IsHighRisk)
                        return View("HighRiskWarning", new HighRiskWarningViewModel
                        {
                            Evaluation = evaluation,
                            Request = model
                        });
                }

                var created = await _loanService.CreateAsync(request, UserId, cancellationToken);

                Success($"{AppMessages.LoanCreated} Número de préstamo: {created.LoanNumber}.");
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Assign", model);
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Assign", model);
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Assign", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmHighRisk(AssignLoanViewModel model,
            CancellationToken cancellationToken = default)
        {
            model.ConfirmHighRisk = true;

            return await Assign(model, cancellationToken);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var loan = await _loanService.GetDetailAsync(id, cancellationToken);
            if (loan == null)
            {
                Error(AppMessages.LoanNotFound);
                return RedirectToAction(nameof(Index));
            }

            return View(loan);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var loan = await _loanService.GetDetailAsync(id, cancellationToken);
            if (loan == null)
            {
                Error(AppMessages.LoanNotFound);
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(loan.Status, "Activo", StringComparison.OrdinalIgnoreCase))
            {
                Error(AppMessages.LoanNotActive);
                return RedirectToAction(nameof(Index));
            }

            return View(new EditLoanRateViewModel
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                ClientFullName = loan.ClientFullName,

                AnnualInterestRate = Rate.Trim(loan.AnnualInterestRate)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditLoanRateViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _loanService.UpdateRateAsync(model.LoanId, model.AnnualInterestRate, UserId,
                    cancellationToken);

                Success(AppMessages.LoanRateUpdated);
                return RedirectToAction(nameof(Details), new { id = model.LoanId });
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

        private static CreateLoanDto ToDto(AssignLoanViewModel model) => new CreateLoanDto
        {
            ClientId = model.ClientId,
            CapitalAmount = model.CapitalAmount,
            TermInMonths = model.TermInMonths,
            AnnualInterestRate = model.AnnualInterestRate,
            ConfirmHighRisk = model.ConfirmHighRisk
        };

        private async Task<List<ClientOptionViewModel>> LoadClientsAsync(string? identification,
            CancellationToken cancellationToken)
        {
            var clients = await _userReadService.GetClientsAsync(onlyActive: true);

            if (!string.IsNullOrWhiteSpace(identification))
            {
                var needle = identification.Trim();
                clients = clients.Where(c => c.Identification.Contains(needle)).ToList();
            }

            var options = new List<ClientOptionViewModel>(clients.Count);

            foreach (var client in clients)
            {
                var loans = await _loanService.GetByClientAsync(client.Id, onlyActive: true, cancellationToken);
                var accounts = await _accountService.GetActiveByClientAsync(client.Id, cancellationToken);

                options.Add(new ClientOptionViewModel
                {
                    Id = client.Id,
                    FullName = client.FullName,
                    Identification = client.Identification,
                    Email = client.Email,
                    HasActiveLoan = loans.Count > 0,
                    HasPrincipalAccount = accounts.Any(a => a.IsPrincipal)
                });
            }

            return options;
        }
    }
}
