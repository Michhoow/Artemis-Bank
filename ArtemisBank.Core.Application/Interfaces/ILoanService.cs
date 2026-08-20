using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Reglas de negocio de prestamos (sistema frances, riesgo, desembolso, edicion de tasa, mora).
    /// Dueno: Manuel.
    /// </summary>
    public interface ILoanService
    {
        Task<PagedResult<LoanListItemDto>> GetPagedAsync(LoanFilterDto filter,
            CancellationToken cancellationToken = default);

        /// <summary>Como GetPagedAsync pero con el mensaje informativo del documento funcional.</summary>
        Task<(PagedResult<LoanListItemDto> Result, string? InfoMessage)> SearchAsync(LoanFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<LoanDetailDto?> GetDetailAsync(int loanId, CancellationToken cancellationToken = default);

        /// <summary>Clientes activos sin prestamo activo (pantalla de seleccion).</summary>
        Task<List<Dtos.Common.UserInfoDto>> GetEligibleClientsAsync(string? identification,
            CancellationToken cancellationToken = default);

        /// <summary>Deuda promedio de los clientes activos (prestamos activos + tarjetas activas).</summary>
        Task<decimal> GetAverageActiveClientDebtAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea el prestamo: valida, evalua riesgo, genera amortizacion y desembolsa a la cuenta principal.
        /// Si el cliente es de alto riesgo y no se confirmo, lanza HighRiskConflictException (→ 409).
        /// </summary>
        Task<LoanCreatedDto> AssignAsync(AssignLoanDto request, CancellationToken cancellationToken = default);

        /// <summary>Modifica la tasa anual y recalcula solo cuotas futuras pendientes.</summary>
        Task UpdateRateAsync(int loanId, decimal newAnnualRate, CancellationToken cancellationToken = default);

        /// <summary>Proceso diario (Azure Function): marca/desmarca cuotas atrasadas. Devuelve cuantas cambiaron.</summary>
        Task<int> RefreshOverdueInstallmentsAsync(DateTime asOf, CancellationToken cancellationToken = default);
    }
}
