using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using ArtemisBank.WebApp.Common;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class SavingsAccountController : BaseController
    {
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly IUserReadService _userReadService;
        private readonly ILoanReadService _loanReadService;
        private readonly ICreditCardReadService _creditCardReadService;
        private readonly IMapper _mapper;

        public SavingsAccountController(
            ISavingsAccountService savingsAccountService,
            IUserReadService userReadService,
            ILoanReadService loanReadService,
            ICreditCardReadService creditCardReadService,
            IMapper mapper,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _savingsAccountService = savingsAccountService;
            _userReadService = userReadService;
            _loanReadService = loanReadService;
            _creditCardReadService = creditCardReadService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(string? identification, string? status, string? type,
            int page = 1, CancellationToken cancellationToken = default)
        {
            var (result, infoMessage) = await _savingsAccountService.SearchAsync(new SavingsAccountFilterDto
            {
                Page = page,
                PageSize = PagedResult<SavingsAccountDto>.MaxPageSize,
                Identification = identification,
                Status = status,
                Type = type
            }, cancellationToken);

            return View(new SavingsAccountIndexViewModel
            {
                Accounts = ToViewModel(result),
                Identification = identification,
                Status = string.IsNullOrWhiteSpace(status) ? "activa" : status,
                Type = string.IsNullOrWhiteSpace(type) ? "todas" : type,
                InfoMessage = infoMessage
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectClient(string? search, CancellationToken cancellationToken)
            => View(new SelectClientViewModel { Search = search, Clients = await LoadClientsAsync(search) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectClient(SelectClientViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                model.Clients = await LoadClientsAsync(model.Search);
                return View(model);
            }

            var client = await _userReadService.GetByIdAsync(model.ClientId!);

            if (client == null)
            {
                ModelState.AddModelError(nameof(model.ClientId), AppMessages.MustSelectClient);
            }
            else if (!client.IsActive)
            {
                ModelState.AddModelError(nameof(model.ClientId), AppMessages.OnlyActiveClients);
            }
            else
            {
                var accounts = await _savingsAccountService.GetActiveByClientAsync(client.Id, cancellationToken);
                if (!accounts.Any(a => a.Type == "Principal"))
                    ModelState.AddModelError(nameof(model.ClientId), AppMessages.ClientNeedsPrincipalAccount);
            }

            if (!ModelState.IsValid)
            {
                model.Clients = await LoadClientsAsync(model.Search);
                return View(model);
            }

            return RedirectToAction(nameof(Assign), new { clientId = model.ClientId });
        }

        [HttpGet]
        public async Task<IActionResult> Assign(string clientId)
        {
            var client = await _userReadService.GetByIdAsync(clientId);
            if (client == null)
            {
                Error(AppMessages.MustSelectClient);
                return RedirectToAction(nameof(SelectClient));
            }

            return View(new AssignSavingsAccountViewModel
            {
                ClientId = client.Id,
                ClientFullName = client.FullName,
                Identification = client.Identification,
                InitialBalance = 0m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignSavingsAccountViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var created = await _savingsAccountService.CreateSecondaryAsync(
                    model.ClientId, model.InitialBalance ?? 0m, UserId, cancellationToken);

                Success($"Cuenta de ahorro {created.AccountNumber} asignada correctamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is BusinessRuleException or NotFoundException or ConflictException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(string accountNumber, int page = 1,
            CancellationToken cancellationToken = default)
        {
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

        [HttpGet]
        public async Task<IActionResult> Cancel(string accountNumber, CancellationToken cancellationToken)
        {
            var account = await _savingsAccountService.GetByAccountNumberAsync(accountNumber, cancellationToken);

            if (account == null)
            {
                Error(AppMessages.AccountDoesNotExist);
                return RedirectToAction(nameof(Index));
            }

            if (account.Type == "Principal")
            {
                Error(AppMessages.PrincipalCannotBeCancelled);
                return RedirectToAction(nameof(Index));
            }

            if (account.Status == "Cancelada")
            {
                Error(AppMessages.AccountAlreadyCancelled);
                return RedirectToAction(nameof(Index));
            }

            return View(new CancelAccountViewModel
            {
                AccountNumber = account.AccountNumber,
                ClientFullName = account.ClientFullName,
                Balance = account.Balance
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Cancel))]
        public async Task<IActionResult> CancelConfirmed(string accountNumber, CancellationToken cancellationToken)
        {
            try
            {
                await _savingsAccountService.CancelSecondaryAsync(accountNumber, UserId, cancellationToken);
                Success($"La cuenta {accountNumber} fue cancelada correctamente.");
            }
            catch (Exception ex) when (ex is BusinessRuleException or NotFoundException)
            {
                Error(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private PagedResult<SavingsAccountViewModel> ToViewModel(PagedResult<SavingsAccountDto> source)
            => PagedResult<SavingsAccountViewModel>.Create(
                _mapper.Map<List<SavingsAccountViewModel>>(source.Data),
                source.Page, source.PageSize, source.TotalRecords);

        private async Task<List<ClientRowViewModel>> LoadClientsAsync(string? search)
        {
            var clients = await _userReadService.GetClientsAsync(onlyActive: true);

            if (!string.IsNullOrWhiteSpace(search))
                clients = clients.Where(c => c.Identification.Contains(search.Trim())).ToList();

            var rows = new List<ClientRowViewModel>();

            foreach (var client in clients)
            {
                var debt = await _loanReadService.GetPendingDebtByClientAsync(client.Id)
                           + await _creditCardReadService.GetDebtByClientAsync(client.Id);

                var accounts = await _savingsAccountService.GetActiveByClientAsync(client.Id);

                rows.Add(new ClientRowViewModel
                {
                    ClientId = client.Id,
                    Identification = client.Identification,
                    FullName = client.FullName,
                    Email = client.Email,
                    TotalDebt = Money.Round(debt),
                    HasActivePrincipalAccount = accounts.Any(a => a.Type == "Principal")
                });
            }

            return rows;
        }
    }
}
