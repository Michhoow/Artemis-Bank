using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    public class CreateCommerceApiRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gestión de comercios para Administradores en Web API.
    /// Propiedad: Monserrat (DoD MON-04).
    /// </summary>
    [Authorize(Roles = DefaultRoles.Administrador)]
    public class CommerceController : BaseApiController
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly IAccountServiceForWebApi _accountService;

        public CommerceController(
            ICommerceRepository commerceRepository,
            IAccountServiceForWebApi accountService)
        {
            _commerceRepository = commerceRepository;
            _accountService     = accountService;
        }

        /// <summary>Listado de todos los comercios registrados.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var commerces = await _commerceRepository.GetAllAsync();
            return Ok(commerces);
        }

        /// <summary>Obtiene un comercio por su ID.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var commerce = await _commerceRepository.GetByIdAsync(id);
            if (commerce is null) return NotFound(new { error = "Comercio no encontrado." });
            return Ok(commerce);
        }

        /// <summary>Crea un nuevo comercio (usuario de rol Comercio + cuenta principal + registro de Comercio).</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCommerceApiRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Validar unicidad de RNC
            if (await _commerceRepository.RncExistsAsync(request.Rnc))
                return BadRequest(new { error = "El RNC especificado ya está registrado." });

            // Validar unicidad de Correo
            if (await _commerceRepository.EmailExistsAsync(request.Email))
                return BadRequest(new { error = "El correo del comercio ya está registrado." });

            // Crear el usuario con el rol Comercio
            var (success, error, user) = await _accountService.RegisterAsync(
                request.Name, "", request.Rnc, request.Email,
                request.UserName, request.Password, DefaultRoles.Comercio);

            if (!success)
                return BadRequest(new { error });

            var commerce = await _commerceRepository.GetByUserIdAsync(user!.Id);

            return CreatedAtAction(nameof(GetById), new { id = commerce?.Id }, commerce);
        }
    }
}
