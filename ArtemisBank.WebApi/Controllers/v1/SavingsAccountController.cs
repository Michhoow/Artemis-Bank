using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Features.SavingsAccounts.Commands;
using ArtemisBank.Core.Application.Features.SavingsAccounts.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Administracion de cuentas de ahorro desde la Web API.
    /// Todos los endpoints requieren JWT y rol Administrador.
    /// La orquestacion se hace por CQRS: el controlador solo envia Commands y Queries por Mediator.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/savings-account")]
    [Authorize(Roles = "Administrador")]
    [Produces("application/json")]
    public class SavingsAccountController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;

        public SavingsAccountController(IAuthenticatedUser currentUser) => _currentUser = currentUser;

        /// <summary>Obtiene un listado paginado de cuentas de ahorro.</summary>
        /// <remarks>
        /// Por defecto muestra las cuentas activas, principales y secundarias, de la mas reciente
        /// a la mas antigua. Permite filtrar por cedula del cliente, estado y tipo de cuenta.
        /// </remarks>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="identification">Cedula del cliente.</param>
        /// <param name="status">activa | cancelada | todas.</param>
        /// <param name="type">principal | secundaria | todas.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SavingsAccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? identification = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetSavingsAccountsQuery
            {
                Page = page,
                PageSize = pageSize,
                Identification = identification,
                Status = status,
                Type = type
            }, cancellationToken);

            return Ok(result);
        }

        /// <summary>Asigna una nueva cuenta de ahorro secundaria a un cliente activo.</summary>
        /// <remarks>
        /// Este endpoint nunca crea cuentas principales: esas se generan automaticamente al crear
        /// el usuario de tipo Cliente o Comercio. El cliente debe tener una cuenta principal activa.
        /// Si el balance inicial es mayor que RD$0.00 se registra una transaccion inicial de tipo CREDITO.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(SavingsAccountDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] CreateSavingsAccountDto request,
            CancellationToken cancellationToken)
        {
            var created = await Mediator.Send(new CreateSecondarySavingsAccountCommand
            {
                ClientId = request.ClientId,
                InitialBalance = request.InitialBalance,
                CreatedByUserId = _currentUser.UserId
            }, cancellationToken);

            return CreatedAtAction(nameof(GetTransactions),
                new { accountNumber = created.AccountNumber }, created);
        }

        /// <summary>Obtiene la cuenta y su historial de transacciones paginado.</summary>
        /// <remarks>Las transacciones se devuelven de la mas reciente a la mas antigua.</remarks>
        /// <param name="accountNumber">Numero identificador de 9 digitos de la cuenta.</param>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Transacciones por pagina. Maximo 20.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet("{accountNumber}/transactions")]
        [ProducesResponseType(typeof(SavingsAccountTransactionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(string accountNumber,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetAccountTransactionsQuery
            {
                AccountNumber = accountNumber,
                Page = page,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(result);
        }

        /// <summary>Cancela una cuenta de ahorro secundaria activa.</summary>
        /// <remarks>
        /// Las cuentas principales no pueden cancelarse. Si la cuenta secundaria tiene balance
        /// disponible, se transfiere automaticamente a la cuenta principal activa del mismo cliente
        /// registrando un DEBITO en la secundaria y un CREDITO en la principal.
        /// La cuenta nunca se elimina fisicamente ni pierde su historial.
        /// </remarks>
        /// <param name="accountNumber">Numero identificador de 9 digitos de la cuenta a cancelar.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPatch("{accountNumber}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(string accountNumber, CancellationToken cancellationToken)
        {
            await Mediator.Send(new CancelSavingsAccountCommand
            {
                AccountNumber = accountNumber,
                CancelledByUserId = _currentUser.UserId
            }, cancellationToken);

            return NoContent();
        }
    }
}
