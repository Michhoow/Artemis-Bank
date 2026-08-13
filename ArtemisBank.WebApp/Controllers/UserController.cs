using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = ArtemisRoles.Cliente;
    }

    /// <summary>
    /// Controlador MVC para la gestión de usuarios por parte del Administrador.
    /// Propiedad: Monserrat (DoD MON-04).
    /// </summary>
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class UserController : Controller
    {
        private readonly IUserReadService _userReadService;
        private readonly IAccountServiceForWebApp _accountService;

        public UserController(
            IUserReadService userReadService,
            IAccountServiceForWebApp accountService)
        {
            _userReadService = userReadService;
            _accountService  = accountService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 20;
            if (page < 1) page = 1;

            var clients = await _userReadService.GetClientsAsync(onlyActive: false);

            ViewBag.TotalCount = clients.Count;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(clients.Count / (double)pageSize);

            var paged = clients
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserViewModel
                {
                    Id             = u.Id,
                    FirstName      = u.FirstName,
                    LastName       = u.LastName,
                    Identification = u.Identification,
                    Email          = u.Email,
                    UserName       = u.UserName,
                    Role           = u.Role,
                    IsActive       = u.IsActive
                })
                .ToList();

            return View(paged);
        }

        public IActionResult Create() => View(new CreateUserViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                return View(model);
            }

            var (success, error, user) = await _accountService.RegisterAsync(
                model.FirstName, model.LastName, model.Identification,
                model.Email, model.UserName, model.Password, model.Role);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al crear el usuario.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Usuario '{model.UserName}' creado exitosamente. Se envió correo de activación.";
            return RedirectToAction(nameof(Index));
        }
    }
}
