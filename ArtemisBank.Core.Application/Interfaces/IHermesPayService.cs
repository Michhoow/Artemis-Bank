using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Procesador de pago Hermes Pay: cobra a una tarjeta y acredita a la cuenta principal del comercio.
    /// La resolucion del comercio (URL vs JWT segun rol) la hace el controlador antes de llegar aqui.
    /// Dueno: Manuel.
    /// </summary>
    public interface IHermesPayService
    {
        Task<PagedResult<CommerceTransactionDto>> GetTransactionsAsync(int commerceId, int page, int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Procesa el pago de forma transaccional. Un rechazo por crédito insuficiente se registra RECHAZADO.</summary>
        Task<OperationResult> ProcessPaymentAsync(ProcessPaymentDto request,
            CancellationToken cancellationToken = default);
    }
}
