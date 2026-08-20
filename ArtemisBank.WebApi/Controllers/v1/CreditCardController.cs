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
    /// Administracion de tarjetas de credito desde la Web API. Propiedad: Manuel.
    /// Todos los endpoints requieren JWT y rol Administrador.
    /// Nunca se expone el numero completo de la tarjeta ni el CVC.
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
        /// Por defecto muestra las tarjetas activas, de la mas reciente a la mas antigua. Permite
        /// filtrar por cedula del cliente y por estado (activa, cancelada, todas). El numero se
        /// muestra siempre enmascarado.
        /// </remarks>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="identification">Cedula del cliente.</param>
        /// <param name="status">activa | cancelada | todas.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CreditCardListItemDto>), StatusCodes.Status200OK)]
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

        /// <summary>Obtiene el detalle de una tarjeta con su historial de consumos.</summary>
        /// <param name="id">Identificador de la tarjeta.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CreditCardDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var card = await Mediator.Send(new GetCreditCardDetailQuery { Id = id }, cancellationToken);
            if (card == null) return NotFound();
            return Ok(card);
        }

        /// <summary>Emite una nueva tarjeta de credito a un cliente activo.</summary>
        /// <remarks>
        /// El sistema genera un numero de 16 digitos unico, una fecha de expiracion a 3 anios y un CVC
        /// aleatorio que se almacena solo como hash. La deuda inicial es RD$0.00.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(CreditCardListItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Post([FromBody] AssignCreditCardApiRequest request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new AssignCreditCardCommand
            {
                ClientId = request.ClientId,
                CreditLimit = request.CreditLimit,
                AssignedByUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Modifica el limite de credito de una tarjeta activa.</summary>
        /// <remarks>El nuevo limite no puede ser inferior a la deuda actual de la tarjeta.</remarks>
        /// <param name="id">Identificador de la tarjeta.</param>
        [HttpPatch("{id:int}/limit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateCardLimitApiRequest request,
            CancellationToken cancellationToken)
        {
            await Mediator.Send(new UpdateCardLimitCommand
            {
                Id = id,
                CreditLimit = request.CreditLimit
            }, cancellationToken);

            return NoContent();
        }

        /// <summary>Cancela una tarjeta de credito activa.</summary>
        /// <remarks>
        /// Solo se puede cancelar si la deuda es RD$0.00. La tarjeta nunca se elimina fisicamente:
        /// pasa a estado Cancelada y conserva su historial de consumos.
        /// </remarks>
        /// <param name="id">Identificador de la tarjeta.</param>
        [HttpPatch("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            await Mediator.Send(new CancelCardCommand { Id = id }, cancellationToken);
            return NoContent();
        }
    }

    /// <summary>Cuerpo de la emision de tarjeta.</summary>
    public class AssignCreditCardApiRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
    }

    /// <summary>Cuerpo de la modificacion de limite.</summary>
    public class UpdateCardLimitApiRequest
    {
        public decimal CreditLimit { get; set; }
    }
}
