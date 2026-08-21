using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.WebApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    [Authorize(Roles = ArtemisRoles.Administrador)]
    public class AdminHomeController : BaseController
    {
        private readonly IAdminHomeService _adminHomeService;

        public AdminHomeController(IAdminHomeService adminHomeService, IAuthenticatedUser currentUser)
            : base(currentUser)
        {
            _adminHomeService = adminHomeService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
            => View(await _adminHomeService.GetIndicatorsAsync(cancellationToken));
    }
}
