using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.Users;
using ArtemisBank.Core.Application.Features.Users.Commands;
using ArtemisBank.Core.Application.Features.Users.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Gestion de usuarios desde la Web API.
    /// Todos los endpoints requieren JWT y rol Administrador.
    ///
    /// Los usuarios creados por esta via nacen INACTIVOS y reciben un correo con el TOKEN de
    /// activacion en el cuerpo del mensaje (no un enlace), porque la API no tiene pantallas
    /// a las cuales redirigir.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/users")]
    [Authorize(Roles = "Administrador")]
    [Produces("application/json")]
    public class UsersController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;

        public UsersController(IAuthenticatedUser currentUser) => _currentUser = currentUser;

        /// <summary>Obtiene el listado paginado de usuarios.</summary>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="role">Filtro opcional: Administrador, Cajero, Cliente o Comercio.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? role = null,
            CancellationToken cancellationToken = default)
            => Ok(await Mediator.Send(new GetUsersQuery { Page = page, PageSize = pageSize, Role = role },
                cancellationToken));

        /// <summary>Crea un usuario con rol Administrador, Cajero o Cliente.</summary>
        /// <remarks>
        /// El usuario, el correo y la cedula deben ser unicos; si ya existen la respuesta es
        /// 409 Conflict.
        ///
        /// El usuario se crea inactivo y recibe por correo el token de activacion.
        /// Si el rol es Cliente se genera automaticamente su cuenta de ahorro principal con un
        /// numero unico de 9 digitos; si <c>initialAmount</c> es mayor que cero, ese balance
        /// entra como una transaccion de tipo CREDITO.
        ///
        /// El rol Comercio no puede crearse aqui: use <c>POST /api/users/commerce/{commerceId}</c>.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(CreatedUserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] CreateUserApiDto request,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new CreateUserCommand
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                Role = request.Role,
                InitialAmount = request.InitialAmount,
                CreatedByUserId = _currentUser.UserId,
                // En la API el correo lleva el token, no un enlace.
                SendTokenInsteadOfLink = true
            }, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "No fue posible crear el usuario.",
                    Detail = result.Error
                });

            var response = new CreatedUserResponseDto
            {
                Id = result.UserId ?? string.Empty,
                UserName = request.UserName,
                Email = request.Email,
                Role = request.Role,
                IsActive = false
            };

            return CreatedAtAction(nameof(Get), new { }, response);
        }

        /// <summary>Crea el usuario con rol Comercio de un comercio existente.</summary>
        /// <remarks>
        /// Cada comercio admite un unico usuario asociado. Si el comercio ya tiene uno,
        /// la respuesta es 409 Conflict.
        ///
        /// Si el comercio aun no tiene cuenta de ahorro principal, se le genera una para que
        /// pueda recibir los cobros de Hermes Pay.
        /// </remarks>
        /// <param name="commerceId">Identificador del comercio al que se asociara el usuario.</param>
        /// <param name="request">Datos del usuario de comercio.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPost("commerce/{commerceId:int}")]
        [ProducesResponseType(typeof(CreatedUserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PostCommerceUser(int commerceId,
            [FromBody] CreateCommerceUserApiDto request, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new CreateCommerceUserCommand
            {
                CommerceId = commerceId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                SendTokenInsteadOfLink = true
            }, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "No fue posible crear el usuario de comercio.",
                    Detail = result.Error
                });

            return CreatedAtAction(nameof(Get), new { }, new CreatedUserResponseDto
            {
                Id = result.UserId ?? string.Empty,
                UserName = request.UserName,
                Email = request.Email,
                Role = "Comercio",
                IsActive = false
            });
        }

        /// <summary>Actualiza los datos de un usuario.</summary>
        /// <remarks>
        /// El ROL no forma parte del cuerpo y no puede modificarse: cambiarlo dejaria huerfanos
        /// los productos financieros ya asociados al usuario.
        ///
        /// Para clientes, un <c>additionalAmount</c> mayor que cero se acredita a su cuenta
        /// principal como transaccion de tipo CREDITO.
        /// </remarks>
        /// <param name="id">Identificador del usuario.</param>
        /// <param name="request">Datos actualizados.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Put(string id, [FromBody] UpdateUserApiDto request,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new UpdateUserCommand
            {
                UserId = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                AdditionalAmount = request.AdditionalAmount,
                UpdatedByUserId = _currentUser.UserId
            }, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "No fue posible actualizar el usuario.",
                    Detail = result.Error
                });

            return NoContent();
        }

        /// <summary>Activa o inactiva un usuario.</summary>
        /// <remarks>
        /// Un usuario no puede activar ni inactivar su propia cuenta: el intento responde
        /// 403 Forbidden.
        /// </remarks>
        /// <param name="id">Identificador del usuario objetivo.</param>
        /// <param name="request">Nuevo estado.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetStatus(string id, [FromBody] UserStatusApiDto request,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new SetUserStatusCommand
            {
                TargetUserId = id,
                IsActive = request.IsActive,
                CallerUserId = _currentUser.UserId
            }, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "No fue posible cambiar el estado del usuario.",
                    Detail = result.Error
                });

            return NoContent();
        }
    }
}
