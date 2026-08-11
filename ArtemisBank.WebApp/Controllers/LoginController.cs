using System.Security.Claims;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Services.Pending;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    /// <summary>
    /// PLACEHOLDER — el login real, la activacion de cuenta y el restablecimiento de contrasenia
    /// son propiedad de Monserrat (modulo de seguridad).
    ///
    /// Michael solo dejo aqui:
    ///   - La pantalla de Acceso denegado, que sus modulos referencian.
    ///   - Un acceso de desarrollo (DevSignIn) que permite probar cuentas, transacciones y cajero
    ///     antes de que Identity este integrado. Se habilita con EnableDevLogin en appsettings
    ///     y debe eliminarse al integrar el login real.
    /// </summary>
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration) => _configuration = configuration;

        public IActionResult Index()
        {
            ViewBag.DevLoginEnabled = _configuration.GetValue<bool>("EnableDevLogin");
            return View();
        }

        public IActionResult AccessDenied() => View();

        /// <summary>TEMPORAL — emite una cookie de prueba para un usuario del directorio de demostracion.</summary>
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

        public async Task<IActionResult> SignOutSession()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }
    }
}
