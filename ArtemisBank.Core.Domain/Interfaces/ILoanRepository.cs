using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    /// <summary>Repositorio de prestamos y su tabla de amortizacion. Propiedad: Manuel.</summary>
    public interface ILoanRepository : IGenericRepository<Loan>
    {
        /// <summary>Prestamo con sus cuotas cargadas (Include Installments).</summary>
        Task<Loan?> GetByIdWithInstallmentsAsync(int loanId, CancellationToken cancellationToken = default);

        Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);

        /// <summary>Prestamo Activo del cliente (a lo sumo uno), con cuotas.</summary>
        Task<Loan?> GetActiveByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default);

        Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>Deuda pendiente (suma del pendiente de cuotas) de los prestamos ACTIVOS del cliente.</summary>
        Task<decimal> GetActivePendingDebtByClientAsync(string clientId, CancellationToken cancellationToken = default);

        /// <summary>Cuotas pendientes de prestamos activos cuya fecha de vencimiento ya paso (para el proceso de mora).</summary>
        Task<List<Installment>> GetOverdueCandidateInstallmentsAsync(DateTime asOf,
            CancellationToken cancellationToken = default);

        /// <summary>Query base para listados paginados (Include Installments).</summary>
        IQueryable<Loan> QueryWithInstallments();
    }
}
