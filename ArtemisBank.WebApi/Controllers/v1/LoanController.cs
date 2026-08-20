using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Features.Loans.Commands;
using ArtemisBank.Core.Application.Features.Loans.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Administracion de prestamos desde la Web API. Propiedad: Manuel.
    /// Todos los endpoints requieren JWT y rol Administrador.
    /// La orquestacion se hace por CQRS: el controlador solo envia Commands y Queries por Mediator.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/loan")]
    [Authorize(Roles = "Administrador")]
    [Produces("application/json")]
    public class LoanController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;

        public LoanController(IAuthenticatedUser currentUser) => _currentUser = currentUser;

        /// <summary>Obtiene un listado paginado de prestamos.</summary>
        /// <remarks>
        /// Por defecto muestra los prestamos activos, del mas reciente al mas antiguo. Permite filtrar
        /// por cedula del cliente y por estado (activos, completados, todos).
        /// </remarks>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="identification">Cedula del cliente.</param>
        /// <param name="status">activos | completados | todos.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<LoanListItemDto>), StatusCodes.Status200OK)]
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
            var result = await Mediator.Send(new GetLoansQuery
            {
                Page = page,
                PageSize = pageSize,
                Identification = identification,
                Status = status
            }, cancellationToken);

            return Ok(result);
        }

        /// <summary>Obtiene el detalle de un prestamo con su tabla de amortizacion completa.</summary>
        /// <param name="id">Identificador del prestamo.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(LoanDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var loan = await Mediator.Send(new GetLoanDetailQuery { Id = id }, cancellationToken);
            if (loan == null) return NotFound();
            return Ok(loan);
        }

        /// <summary>Asigna un prestamo a un cliente y desembolsa el capital a su cuenta principal.</summary>
        /// <remarks>
        /// El plazo debe ser un multiplo de 6 entre 6 y 60 meses. El sistema evalua el riesgo del
        /// cliente comparando su deuda con el promedio de los clientes activos. Si el cliente es o se
        /// convierte en cliente de alto riesgo, se devuelve 409 con el desglose del riesgo; para forzar
        /// la asignacion se debe reenviar con confirmHighRisk = true.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(LoanCreatedDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(HighRiskConflictDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] AssignLoanApiRequest request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new AssignLoanCommand
            {
                ClientId = request.ClientId,
                CapitalAmount = request.CapitalAmount,
                TermInMonths = request.TermInMonths,
                AnnualInterestRate = request.AnnualInterestRate,
                ConfirmHighRisk = request.ConfirmHighRisk,
                AssignedByUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Modifica la tasa de interes anual de un prestamo activo.</summary>
        /// <remarks>
        /// Solo se recalculan las cuotas futuras pendientes (no las pagadas, vencidas ni parciales).
        /// El capital ya amortizado no se altera.
        /// </remarks>
        /// <param name="id">Identificador del prestamo.</param>
        [HttpPatch("{id:int}/rate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateLoanRateApiRequest request,
            CancellationToken cancellationToken)
        {
            await Mediator.Send(new UpdateLoanRateCommand
            {
                Id = id,
                AnnualInterestRate = request.AnnualInterestRate
            }, cancellationToken);

            return NoContent();
        }
    }

    /// <summary>Cuerpo de la asignacion de prestamo.</summary>
    public class AssignLoanApiRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public bool ConfirmHighRisk { get; set; }
    }

    /// <summary>Cuerpo de la modificacion de tasa.</summary>
    public class UpdateLoanRateApiRequest
    {
        public decimal AnnualInterestRate { get; set; }
    }
}
