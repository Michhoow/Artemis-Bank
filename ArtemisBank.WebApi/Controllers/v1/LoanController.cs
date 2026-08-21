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
    /// Gestion de prestamos desde la Web API.
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
        /// Por defecto muestra los prestamos activos, del mas reciente al mas antiguo.
        /// Al buscar por cedula sin especificar estado se listan primero los activos y luego
        /// los completados.
        /// </remarks>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="identification">Cedula del cliente.</param>
        /// <param name="status">activo | completado | todos.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<LoanDto>), StatusCodes.Status200OK)]
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

        /// <summary>Asigna un prestamo a un cliente activo y genera su tabla de amortizacion.</summary>
        /// <remarks>
        /// El cliente debe estar activo, no tener otro prestamo activo y contar con una cuenta de
        /// ahorro principal activa que reciba el desembolso.
        ///
        /// Antes de crear nada se evalua el riesgo crediticio. Si el cliente es o se convierte en
        /// cliente de alto riesgo y <c>confirmHighRisk</c> no viene en <c>true</c>, la respuesta es
        /// <b>409 Conflict</b> con el detalle del riesgo; reenvie la misma solicitud con
        /// <c>confirmHighRisk: true</c> para continuar.
        ///
        /// El capital aprobado se acredita a la cuenta principal como transaccion de tipo CREDITO.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(LoanDetailDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] CreateLoanDto request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new CreateLoanCommand
            {
                ClientId = request.ClientId,
                CapitalAmount = request.CapitalAmount,
                TermInMonths = request.TermInMonths,
                AnnualInterestRate = request.AnnualInterestRate,
                ConfirmHighRisk = request.ConfirmHighRisk,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Obtiene el detalle de un prestamo con su tabla de amortizacion.</summary>
        /// <remarks>
        /// Cada cuota incluye su estado de pago (pendiente, parcialmente pagada o pagada) y su
        /// indicador de atraso, que son dos datos independientes.
        /// </remarks>
        /// <param name="id">Identificador del prestamo.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(LoanDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(await Mediator.Send(new GetLoanByIdQuery { Id = id }, cancellationToken));

        /// <summary>Modifica la tasa de interes anual del prestamo.</summary>
        /// <remarks>
        /// Solo se recalculan las cuotas FUTURAS que esten completamente pendientes.
        /// Las cuotas pagadas, parcialmente pagadas y atrasadas no se modifican, y las fechas
        /// de vencimiento pactadas se conservan.
        /// </remarks>
        /// <param name="id">Identificador del prestamo.</param>
        /// <param name="request">Nueva tasa de interes anual.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{id:int}/rate")]
        [ProducesResponseType(typeof(LoanDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateLoanRateDto request,
            CancellationToken cancellationToken)
        {
            var updated = await Mediator.Send(new UpdateLoanRateCommand
            {
                LoanId = id,
                AnnualInterestRate = request.AnnualInterestRate,
                AdminUserId = _currentUser.UserId
            }, cancellationToken);

            return Ok(updated);
        }
    }
}
