using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Application.Features.Commerces.Commands;
using ArtemisBank.Core.Application.Features.Commerces.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Gestion de comercios desde la Web API.
    /// Todos los endpoints requieren JWT y rol Administrador.
    /// La orquestacion se hace por CQRS: el controlador solo envia Commands y Queries por Mediator
    /// y nunca toca repositorios ni el DbContext.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/commerce")]
    [Authorize(Roles = "Administrador")]
    [Produces("application/json")]
    public class CommerceController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;

        public CommerceController(IAuthenticatedUser currentUser) => _currentUser = currentUser;

        /// <summary>Obtiene el listado paginado de comercios registrados.</summary>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="status">activo | inactivo | todos. Por defecto solo activos.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CommerceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
            => Ok(await Mediator.Send(
                new GetCommercesQuery { Page = page, PageSize = pageSize, Status = status },
                cancellationToken));

        /// <summary>Obtiene el detalle de un comercio.</summary>
        /// <param name="id">Identificador del comercio.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CommerceDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(await Mediator.Send(new GetCommerceByIdQuery { Id = id }, cancellationToken));

        /// <summary>Registra un nuevo comercio.</summary>
        /// <remarks>
        /// El RNC y el correo deben ser unicos entre comercios; si ya existen la respuesta es
        /// 409 Conflict. El comercio se crea activo pero sin usuario asociado: hasta que se le
        /// cree uno mediante <c>POST /api/users/commerce/{commerceId}</c> no puede operar.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] SaveCommerceDto request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new CreateCommerceCommand
            {
                Name = request.Name,
                Rnc = request.Rnc,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Actualiza los datos de un comercio.</summary>
        /// <remarks>
        /// Este endpoint NUNCA modifica el estado del comercio. Para activarlo o desactivarlo
        /// use <c>PATCH /api/commerce/{id}/status</c>.
        /// </remarks>
        /// <param name="id">Identificador del comercio.</param>
        /// <param name="request">Datos actualizados del comercio.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Put(int id, [FromBody] SaveCommerceDto request,
            CancellationToken cancellationToken)
        {
            var updated = await Mediator.Send(new UpdateCommerceCommand
            {
                Id = id,
                Name = request.Name,
                Rnc = request.Rnc,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description
            }, cancellationToken);

            return Ok(updated);
        }

        /// <summary>Activa o desactiva un comercio.</summary>
        /// <remarks>
        /// Al DESACTIVAR el comercio se inactivan tambien sus usuarios asociados, para que no
        /// puedan seguir emitiendo cobros con un token ya emitido.
        ///
        /// Al REACTIVAR el comercio sus usuarios NO se reactivan automaticamente: el administrador
        /// decide caso por caso quien recupera el acceso.
        /// </remarks>
        /// <param name="id">Identificador del comercio.</param>
        /// <param name="request">Nuevo estado del comercio.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetStatus(int id, [FromBody] CommerceStatusDto request,
            CancellationToken cancellationToken)
        {
            var updated = await Mediator.Send(new SetCommerceStatusCommand
            {
                Id = id,
                IsActive = request.IsActive,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return Ok(updated);
        }
    }
}
