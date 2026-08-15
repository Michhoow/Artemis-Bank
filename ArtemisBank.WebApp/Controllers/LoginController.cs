using System.Security.Claims;
using ArtemisBank.Core.Application.Services.Pending;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    public class LoginViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Controlador de autenticación y acceso para la WebApp MVC.
    /// Propiedad: Monserrat (DoD MON-01 + MON-03).
    /// </summary>
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IAccountServiceForWebApp _accountService;
        private readonly IConfiguration _configuration;

        public LoginController(
            IAccountServiceForWebApp accountService,
            IConfiguration configuration)
        {
            _accountService = accountService;
            _configuration  = configuration;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.DevLoginEnabled = _configuration.GetValue<bool>("EnableDevLogin");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DevLoginEnabled = _configuration.GetValue<bool>("EnableDevLogin");
                return View(model);
            }

            var allowedRoles = new[] { ArtemisRoles.Administrador, ArtemisRoles.Cajero, ArtemisRoles.Cliente };
            var (success, error) = await _accountService.LoginAsync(model.UserName, model.Password, allowedRoles);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Credenciales inválidas.");
                ViewBag.DevLoginEnabled = _configuration.GetValue<bool>("EnableDevLogin");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> ConfirmAccount(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Parámetros de activación inválidos.";
                return RedirectToAction(nameof(Index));
            }

            var (success, error) = await _accountService.ConfirmEmailAsync(userId, token);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "No se pudo activar la cuenta.";
            }
            else
            {
                TempData["SuccessMessage"] = "Cuenta activada exitosamente. Ya puede iniciar sesión.";
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
            TempData["SuccessMessage"] = "Si el correo existe en nuestro sistema, recibirá un enlace/código de restablecimiento.";
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

            TempData["SuccessMessage"] = "Contraseña restablecida correctamente. Inicie sesión.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult AccessDenied() => View();

        public async Task<IActionResult> SignOutSession()
        {
            await _accountService.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Acceso temporal para desarrollo cuando EnableDevLogin está activo en appsettings.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevSignIn(string userId)
        {
            if (!_configuration.GetValue<bool>("EnableDevLogin")) return Forbid();

            var demo = DemoDirectory.Users.FirstOrDefault(u => u.Id == userId);
            if (demo == null || !demo.IsActive) return RedirectToAction(nameof(Index));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, demo.Id),
                new Claim(ClaimTypes.Name, demo.UserName),
                new Claim(ClaimTypes.Role, demo.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }
    }
}
