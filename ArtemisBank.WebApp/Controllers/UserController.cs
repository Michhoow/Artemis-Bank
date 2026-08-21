using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.ViewModels.Users;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class UserController : BaseController
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IUserReadService _userReadService;

        public UserController(
            IUserManagementService userManagementService,
            IUserReadService userReadService,
            IAuthenticatedUser currentUser) : base(currentUser)
        {
            _userManagementService = userManagementService;
            _userReadService = userReadService;
        }

        public async Task<IActionResult> Index(string? role, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var users = await _userManagementService.GetPagedAsync(page, 20, role, cancellationToken);

            return View(new UserIndexViewModel
            {
                Users = users,
                Role = role,
                InfoMessage = users.TotalRecords == 0
                    ? "No se encontraron usuarios con el filtro seleccionado."
                    : null
            });
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateUserViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _userManagementService.CreateAsync(new CreateUserRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Identification = model.Identification,
                Email = model.Email,
                UserName = model.UserName,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Role = model.Role,

                InitialAmount = string.Equals(model.Role, "Cliente", StringComparison.OrdinalIgnoreCase)
                    ? model.InitialAmount
                    : null
            }, UserId, sendTokenInsteadOfLink: false, cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "No fue posible crear el usuario.");
                return View(model);
            }

            Success(AppMessages.UserCreated + " El usuario queda inactivo hasta que confirme su correo.");
            Warning(result.Warning);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userReadService.GetByIdAsync(id);
            if (user == null)
            {
                Error(AppMessages.UserNotFound);
                return RedirectToAction(nameof(Index));
            }

            return View(new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Identification = user.Identification,
                Email = user.Email,
                UserName = user.UserName,

                Role = user.Role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _userManagementService.UpdateAsync(new UpdateUserRequest
            {
                UserId = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Identification = model.Identification,
                Email = model.Email,
                UserName = model.UserName,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                AdditionalAmount = model.AdditionalAmount

            }, UserId, cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "No fue posible actualizar el usuario.");
                return View(model);
            }

            Success(AppMessages.UserUpdated);
            Warning(result.Warning);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeStatus(string id)
        {
            var user = await _userReadService.GetByIdAsync(id);
            if (user == null)
            {
                Error(AppMessages.UserNotFound);
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Id, UserId, StringComparison.Ordinal))
            {
                Error(AppMessages.UserCannotModifySelf);
                return RedirectToAction(nameof(Index));
            }

            return View(new ChangeUserStatusViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        [HttpPost, ActionName("ChangeStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatusConfirmed(string id, bool targetStatus,
            CancellationToken cancellationToken = default)
        {
            var result = await _userManagementService.SetActiveAsync(id, targetStatus, UserId,
                cancellationToken);

            if (!result.Succeeded)
                Error(result.Error ?? "No fue posible cambiar el estado del usuario.");
            else
                Success(targetStatus ? AppMessages.UserActivated : AppMessages.UserDeactivated);

            return RedirectToAction(nameof(Index));
        }
    }
}
