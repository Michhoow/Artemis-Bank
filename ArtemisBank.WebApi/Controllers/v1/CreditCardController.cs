using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Features.CreditCards.Commands;
using ArtemisBank.Core.Application.Features.CreditCards.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Gestion de tarjetas de credito desde la Web API.
    /// Todos los endpoints requieren JWT y rol Administrador.
    ///
    /// Ninguna respuesta de este controlador contiene el numero completo de la tarjeta,
    /// el CVC ni su hash. La unica excepcion controlada es el CVC devuelto por POST,
    /// que se entrega una sola vez al crear la tarjeta y no queda almacenado en claro.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/credit-card")]
    [Authorize(Roles = "Administrador")]
    [Produces("application/json")]
    public class CreditCardController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;

        public CreditCardController(IAuthenticatedUser currentUser) => _currentUser = currentUser;

        /// <summary>Obtiene un listado paginado de tarjetas de credito.</summary>
        /// <remarks>
        /// Por defecto muestra las tarjetas activas, de la mas reciente a la mas antigua.
        /// El numero de tarjeta se devuelve siempre enmascarado.
        /// </remarks>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="identification">Cedula del cliente.</param>
        /// <param name="status">activa | cancelada | todas.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CreditCardDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? identification = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetCreditCardsQuery
            {
                Page = page,
                PageSize = pageSize,
                Identification = identification,
                Status = status
            }, cancellationToken);

            return Ok(result);
        }

        /// <summary>Asigna una nueva tarjeta de credito a un cliente activo.</summary>
        /// <remarks>
        /// El sistema genera un numero unico de 16 digitos, una fecha de expiracion a tres anios
        /// y un CVC de 3 digitos.
        ///
        /// El CVC se almacena UNICAMENTE como hash SHA-256 con sal por tarjeta. El valor en claro
        /// se devuelve una sola vez en esta respuesta y no puede recuperarse despues por ningun medio.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(CreatedCreditCardDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] CreateCreditCardDto request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new CreateCreditCardCommand
            {
                ClientId = request.ClientId,
                CreditLimit = request.CreditLimit,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Card.Id }, created);
        }

        /// <summary>Obtiene el detalle de una tarjeta con sus consumos.</summary>
        /// <remarks>
        /// Se incluyen tanto los consumos aprobados como los rechazados, del mas reciente al
        /// mas antiguo. Los rechazados quedan en el historial pero nunca afectaron la deuda.
        /// </remarks>
        /// <param name="id">Identificador de la tarjeta.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CreditCardDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(await Mediator.Send(new GetCreditCardByIdQuery { Id = id }, cancellationToken));

        /// <summary>Modifica el limite de credito aprobado.</summary>
        /// <remarks>
        /// El nuevo limite no puede ser menor que la deuda actual de la tarjeta: dejarlo por
        /// debajo dejaria la tarjeta sobregirada.
        /// </remarks>
        /// <param name="id">Identificador de la tarjeta.</param>
        /// <param name="request">Nuevo limite de credito.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{id:int}/limit")]
        [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateCardLimitDto request,
            CancellationToken cancellationToken)
        {
            var updated = await Mediator.Send(new UpdateCardLimitCommand
            {
                CreditCardId = id,
                CreditLimit = request.CreditLimit,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return Ok(updated);
        }

        /// <summary>Cancela una tarjeta de credito.</summary>
        /// <remarks>
        /// Solo procede si la deuda es exactamente RD$0.00: cancelar una tarjeta con deuda
        /// pendiente borraria un pasivo del cliente. La tarjeta nunca se elimina fisicamente
        /// ni pierde su historial de consumos.
        /// </remarks>
        /// <param name="id">Identificador de la tarjeta.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            await Mediator.Send(new CancelCreditCardCommand
            {
                CreditCardId = id,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return NoContent();
        }
    }
}
