using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArtemisBank.Core.Application.ViewModels.Account;

namespace ArtemisBank.WebApp.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IAccountServiceForWebApp _accountService;

        public LoginController(IAccountServiceForWebApp accountService)
            => _accountService = accountService;

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var allowedRoles = new[] { ArtemisRoles.Administrador, ArtemisRoles.Cajero, ArtemisRoles.Cliente };
            var (success, error) = await _accountService.LoginAsync(model.UserName, model.Password, allowedRoles);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? AppMessages.InvalidCredentials);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> ConfirmAccount(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = AppMessages.ActivationParamsInvalid;
                return RedirectToAction(nameof(Index));
            }

            var (success, error) = await _accountService.ConfirmEmailAsync(userId, token);
            if (!success)
            {
                TempData["Error"] = error ?? "No se pudo activar la cuenta.";
            }
            else
            {
                TempData["Success"] = AppMessages.AccountActivated;
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _accountService.SendPasswordResetEmailAsync(model.Email);
            TempData["Success"] = AppMessages.PasswordResetSent;
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                return View(model);
            }

            var (success, error) = await _accountService.ResetPasswordAsync(
                model.UserId, model.Token, model.NewPassword);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al restablecer la contraseña.");
                return View(model);
            }

            TempData["Success"] = AppMessages.PasswordResetSuccess;
            return RedirectToAction(nameof(Index));
        }

        public IActionResult AccessDenied() => View();

        public async Task<IActionResult> SignOutSession()
        {
            await _accountService.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

    }
}
