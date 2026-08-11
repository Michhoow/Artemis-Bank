using ArtemisBank.Core.Application.Interfaces;
using System.Diagnostics;
using ArtemisBank.WebApp.Common;
using ArtemisBank.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(IAuthenticatedUser currentUser) : base(currentUser) { }

        /// <summary>Redirige al Home correspondiente segun el rol autenticado.</summary>
        public IActionResult Index()
        {
            if (!CurrentUser.IsAuthenticated) return RedirectToAction("Index", "Login");

            return CurrentUser.Role switch
            {
                ArtemisRoles.Administrador => RedirectToAction("Index", "AdminHome"),
                ArtemisRoles.Cajero => RedirectToAction("Index", "Cashier"),
                ArtemisRoles.Cliente => RedirectToAction("Index", "ClientHome"),
                _ => RedirectToAction("AccessDenied", "Login")
            };
        }

        /// <summary>Pantalla de error. Accesible sin autenticacion.</summary>
        [AllowAnonymous]
        public IActionResult Error(int? code) => View(new ErrorViewModel
        {
            StatusCode = code ?? 500,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
