using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.ViewModels.Beneficiaries;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Cliente)]
    public class BeneficiaryController : BaseController
    {
        private readonly IBeneficiaryService _beneficiaryService;

        public BeneficiaryController(IBeneficiaryService beneficiaryService, IAuthenticatedUser currentUser)
            : base(currentUser)
        {
            _beneficiaryService = beneficiaryService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewBag.Beneficiaries = await _beneficiaryService.GetByClientAsync(UserId, cancellationToken);
            return View(new SaveBeneficiaryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveBeneficiaryViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Beneficiaries = await _beneficiaryService.GetByClientAsync(UserId, cancellationToken);
                return View(nameof(Index), model);
            }

            try
            {
                await _beneficiaryService.AddAsync(UserId, model.AccountNumber, cancellationToken);
                Success(AppMessages.BeneficiaryAdded);
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(nameof(model.AccountNumber), ex.Message);
                ViewBag.Beneficiaries = await _beneficiaryService.GetByClientAsync(UserId, cancellationToken);
                return View(nameof(Index), model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var beneficiary = await _beneficiaryService.GetByIdAsync(UserId, id, cancellationToken);
            if (beneficiary == null) return RedirectToAction(nameof(Index));

            return View(beneficiary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Delete))]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _beneficiaryService.DeleteAsync(UserId, id, cancellationToken);
                Success(AppMessages.BeneficiaryDeleted);
            }
            catch (Exception ex) when (ex is NotFoundException or ForbiddenException)
            {
                Error(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
