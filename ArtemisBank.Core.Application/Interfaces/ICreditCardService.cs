using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Reglas de negocio de tarjetas de credito (emision segura, limite, cancelacion) y avance de efectivo.
    /// Dueno: Manuel.
    /// </summary>
    public interface ICreditCardService
    {
        Task<PagedResult<CreditCardListItemDto>> GetPagedAsync(CreditCardFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<(PagedResult<CreditCardListItemDto> Result, string? InfoMessage)> SearchAsync(CreditCardFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<CreditCardDetailDto?> GetDetailAsync(int creditCardId, CancellationToken cancellationToken = default);

        /// <summary>Emite una tarjeta: numero de 16 digitos unico, expiracion +3 anios, CVC hash SHA-256.</summary>
        Task<CreditCardListItemDto> AssignAsync(AssignCreditCardDto request,
            CancellationToken cancellationToken = default);

        /// <summary>Modifica el limite (no puede quedar por debajo de la deuda actual).</summary>
        Task UpdateLimitAsync(int creditCardId, decimal newLimit, CancellationToken cancellationToken = default);

        /// <summary>Cancela una tarjeta activa sin deuda pendiente.</summary>
        Task CancelAsync(int creditCardId, CancellationToken cancellationToken = default);

        /// <summary>Avance de efectivo: acredita a la cuenta y carga monto + 6.25% de interes a la tarjeta.</summary>
        Task<OperationResult> CashAdvanceAsync(CashAdvanceDto request, CancellationToken cancellationToken = default);
    }
}
