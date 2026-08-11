using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    /// <summary>
    /// CONTRATO CONGELADO — lectura y aplicacion de pagos sobre tarjetas de credito.
    /// IMPLEMENTA: Manuel (modulo de tarjetas).
    /// CONSUME: Michael (pago a tarjeta desde cliente y cajero, indicadores).
    /// </summary>
    public interface ICreditCardReadService
    {
        Task<CreditCardInfoDto?> GetByIdAsync(int creditCardId);

        /// <summary>Busqueda por numero completo de 16 digitos (solo la usa el cajero al digitarlo).</summary>
        Task<CreditCardInfoDto?> GetByCardNumberAsync(string cardNumber);

        Task<List<CreditCardInfoDto>> GetActiveByClientAsync(string clientId);
        Task<int> CountActiveAsync();
        Task<decimal> GetDebtByClientAsync(string clientId);

        /// <summary>
        /// Reduce la deuda y actualiza el credito disponible por el monto efectivo pagado.
        /// Michael ya debito la cuenta de origen dentro de la misma transaccion de base de datos.
        /// </summary>
        Task ApplyPaymentAsync(int creditCardId, decimal effectiveAmount, CancellationToken cancellationToken = default);
    }
}
