using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    public class CreateUserApiRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserStatusApiRequestDto
    {
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Endpoint de gestión de usuarios para Administrador en Web API.
    /// Propiedad: Monserrat (DoD MON-04).
    /// </summary>
    [Authorize(Roles = DefaultRoles.Administrador)]
    public class UsersController : BaseApiController
    {
        private readonly IUserReadService _userReadService;
        private readonly IAccountServiceForWebApi _accountService;

        public UsersController(
            IUserReadService userReadService,
            IAccountServiceForWebApi accountService)
        {
            _userReadService = userReadService;
            _accountService  = accountService;
        }

        /// <summary>Listado de usuarios paginado (máx 20 por página, excluye el rol Comercio).</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (pageSize > 20) pageSize = 20;
            if (page < 1) page = 1;

            var clients = await _userReadService.GetClientsAsync(onlyActive: false);
            // Filtrar excluyendo Comercios
            var filtered = clients.Where(u => u.Role != DefaultRoles.Comercio).ToList();

            var total = filtered.Count;
            var paged = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                total,
                page,
                pageSize,
                data = paged
            });
        }

        /// <summary>Crea un nuevo usuario (Cliente/Cajero/Admin) con estado inactivo.</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserApiRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error, user) = await _accountService.RegisterAsync(
                request.FirstName, request.LastName, request.Identification,
                request.Email, request.UserName, request.Password, request.Role);

            if (!success)
                return BadRequest(new { error });

            return CreatedAtAction(nameof(GetUsers), new { id = user?.Id }, new
            {
                id = user?.Id,
                userName = user?.UserName,
                email = user?.Email,
                isActive = user?.IsActive
            });
        }

        /// <summary>Activa o desactiva la cuenta de un usuario.</summary>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusApiRequestDto request)
        {
            var callerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var (success, error) = await _accountService.SetActiveStatusAsync(id, request.IsActive, callerId);
            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = $"Estado del usuario actualizado a IsActive={request.IsActive}." });
        }
    }
}
