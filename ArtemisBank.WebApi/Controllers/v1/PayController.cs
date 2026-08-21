using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.HermesPay;
using ArtemisBank.Core.Application.Features.HermesPay.Commands;
using ArtemisBank.Core.Application.Features.HermesPay.Queries;
using ArtemisBank.Core.Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    /// <summary>
    /// Hermes Pay — procesador de pagos con tarjeta de credito para comercios.
    ///
    /// Accesible por los roles Administrador y Comercio. La resolucion del comercio depende
    /// del rol autenticado y es el punto de seguridad central de este modulo:
    ///
    ///  - Rol <b>Comercio</b>: el comercio se toma SIEMPRE del usuario del token. El
    ///    <c>commerceId</c> de la URL se IGNORA, para que un comercio no pueda operar sobre
    ///    otro simplemente cambiando un numero en la ruta.
    ///  - Rol <b>Administrador</b>: el comercio se toma de la URL, porque opera en nombre
    ///    de cualquiera.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("pay")]
    [Authorize(Roles = "Administrador,Comercio")]
    [Produces("application/json")]
    public class PayController : BaseApiController
    {
        private readonly IAuthenticatedUser _currentUser;
        private readonly ICommerceService _commerceService;

        public PayController(IAuthenticatedUser currentUser, ICommerceService commerceService)
        {
            _currentUser = currentUser;
            _commerceService = commerceService;
        }

        /// <summary>Obtiene las transacciones registradas para un comercio.</summary>
        /// <remarks>
        /// Devuelve un listado paginado, de la transaccion mas reciente a la mas antigua,
        /// junto al identificador y el nombre del comercio.
        ///
        /// Para el rol Comercio, el <c>commerceId</c> de la URL se ignora y se usa el comercio
        /// asociado al token. Un comercio inactivo no puede consultar transacciones.
        ///
        /// De la tarjeta utilizada solo se devuelven los ultimos cuatro digitos.
        /// </remarks>
        /// <param name="commerceId">Identificador del comercio.</param>
        /// <param name="page">Numero de pagina. Debe ser mayor que cero.</param>
        /// <param name="pageSize">Registros por pagina. Maximo 20.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpGet("get-transactions/{commerceId:int}")]
        [ProducesResponseType(typeof(CommerceTransactionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(int commerceId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var resolved = await ResolveCommerceIdAsync(commerceId, cancellationToken);

            return Ok(await Mediator.Send(new GetCommerceTransactionsQuery
            {
                CommerceId = resolved,
                Page = page,
                PageSize = pageSize
            }, cancellationToken));
        }

        /// <summary>Procesa un pago con tarjeta de credito a favor de un comercio.</summary>
        /// <remarks>
        /// Validaciones aplicadas antes de mover dinero: el numero de tarjeta debe tener
        /// 16 digitos; el mes de expiracion debe estar entre 01 y 12 y coincidir con la tarjeta;
        /// el CVC se valida comparando su hash contra el almacenado; la tarjeta debe estar activa
        /// y no vencida; el monto debe ser mayor que cero; y la deuda mas el nuevo consumo no
        /// puede superar el limite aprobado.
        ///
        /// Un cobro aprobado aumenta la deuda de la tarjeta, registra un consumo APROBADO,
        /// acredita el monto en la cuenta principal del comercio como CREDITO y responde
        /// <b>204 No Content</b>. Todo ocurre dentro de una unica transaccion de base de datos.
        ///
        /// Un cobro rechazado por falta de credito queda registrado como consumo RECHAZADO,
        /// no modifica balances ni deudas, y responde <b>400 Bad Request</b>.
        ///
        /// El CVC nunca se almacena, ni se registra en log, ni se devuelve.
        /// </remarks>
        /// <param name="commerceId">
        /// Comercio beneficiario. Se ignora cuando el rol autenticado es Comercio.
        /// </param>
        /// <param name="request">Datos de la tarjeta y el monto a cobrar.</param>
        /// <param name="cancellationToken">Token de cancelacion de la solicitud.</param>
        [HttpPost("process-payment/{commerceId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessPayment(int commerceId,
            [FromBody] HermesPayRequestDto request, CancellationToken cancellationToken)
        {
            var resolved = await ResolveCommerceIdAsync(commerceId, cancellationToken);

            await Mediator.Send(new ProcessPaymentCommand
            {
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                Cvc = request.Cvc,
                TransactionAmount = request.TransactionAmount,
                CommerceId = resolved,
                PerformedByUserId = _currentUser.UserId
            }, cancellationToken);

            return NoContent();
        }

        /// <summary>Determina el comercio segun el rol del token.</summary>
        private async Task<int> ResolveCommerceIdAsync(int commerceIdFromUrl,
            CancellationToken cancellationToken)
        {
            var isCommerce = string.Equals(_currentUser.Role, "Comercio",
                StringComparison.OrdinalIgnoreCase);

            if (!isCommerce) return commerceIdFromUrl;

            var commerce = await _commerceService.GetByUserIdAsync(
                _currentUser.UserId ?? string.Empty, cancellationToken);

            if (commerce == null)
                throw new ForbiddenException(AppMessages.HermesCommerceNotAssociated);

            // Se ignora deliberadamente commerceIdFromUrl.
            return commerce.Id;
        }
    }
}
