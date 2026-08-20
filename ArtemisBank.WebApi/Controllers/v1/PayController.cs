using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Features.HermesPay.Commands;
using ArtemisBank.Core.Application.Features.HermesPay.Queries;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Hermes Pay: procesador de pagos con tarjeta de credito hacia comercios. Propiedad: Manuel.
    ///
    /// Doble rol:
    ///   - Administrador: usa el commerceId de la URL (puede operar sobre cualquier comercio).
    ///   - Comercio: se ignora el commerceId de la URL y se toma el comercio del usuario autenticado (JWT).
    /// </summary>
    [ApiVersion("1.0")]
    [Route("pay")]
    [Authorize(Roles = "Administrador,Comercio")]
    [Produces("application/json")]
    public class PayController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;
        private readonly ICommerceRepository _commerceRepository;

        public PayController(IAuthenticatedUser currentUser, ICommerceRepository commerceRepository)
        {
            _currentUser = currentUser;
            _commerceRepository = commerceRepository;
        }

        /// <summary>Procesa un pago con tarjeta de credito hacia un comercio.</summary>
        /// <remarks>
        /// Valida la tarjeta (existencia, estado, vigencia y CVC), verifica el credito disponible y,
        /// si todo es correcto, aumenta la deuda de la tarjeta, registra el consumo y acredita a la
        /// cuenta principal del comercio. Un rechazo por credito insuficiente se registra sin afectar balances.
        /// </remarks>
        /// <param name="commerceId">Comercio receptor (se ignora si el rol es Comercio).</param>
        [HttpPost("process-payment/{commerceId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessPayment(int commerceId,
            [FromBody] ProcessPaymentApiRequest request, CancellationToken cancellationToken)
        {
            var (effectiveCommerceId, isCommerce) = await ResolveCommerceIdAsync(commerceId);
            if (effectiveCommerceId == null) return Forbid();

            var result = await Mediator.Send(new ProcessPaymentCommand
            {
                CommerceId = effectiveCommerceId.Value,
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                Cvc = request.Cvc,
                TransactionAmount = request.TransactionAmount,
                AuthenticatedUserId = _currentUser.UserId,
                IsCommerceRole = isCommerce
            }, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(new ProblemDetails { Title = "Pago rechazado", Detail = result.ErrorMessage });

            return Ok(new { message = "Pago procesado correctamente.", reference = result.Reference });
        }

        /// <summary>Obtiene las transacciones aprobadas recibidas por un comercio.</summary>
        /// <param name="commerceId">Comercio consultado (se ignora si el rol es Comercio).</param>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        [HttpGet("get-transactions/{commerceId:int}")]
        [ProducesResponseType(typeof(PagedResult<CommerceTransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(int commerceId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var (effectiveCommerceId, _) = await ResolveCommerceIdAsync(commerceId);
            if (effectiveCommerceId == null) return Forbid();

            var result = await Mediator.Send(new GetCommerceTransactionsQuery
            {
                CommerceId = effectiveCommerceId.Value,
                Page = page,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Resuelve el comercio efectivo segun el rol. Para Comercio se toma su propio comercio del JWT
        /// (ignorando la URL); para Administrador se usa el commerceId recibido.
        /// </summary>
        private async Task<(int? CommerceId, bool IsCommerce)> ResolveCommerceIdAsync(int urlCommerceId)
        {
            var role = _currentUser.Role ?? string.Empty;

            if (string.Equals(role, "Comercio", StringComparison.OrdinalIgnoreCase))
            {
                var commerce = await _commerceRepository.GetByUserIdAsync(_currentUser.UserId ?? string.Empty);
                return commerce == null ? (null, true) : (commerce.Id, true);
            }

            // Administrador (u otro rol autorizado): usa el commerceId de la URL.
            return (urlCommerceId, false);
        }
    }

    /// <summary>Cuerpo del procesamiento de pago Hermes Pay.</summary>
    public class ProcessPaymentApiRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public string MonthExpirationCard { get; set; } = string.Empty;
        public string YearExpirationCard { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
    }
}
