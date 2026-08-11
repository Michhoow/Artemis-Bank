using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    /// <summary>
    /// CONTRATO CONGELADO — lectura y aplicacion de pagos sobre prestamos.
    /// IMPLEMENTA: Manuel (modulo de prestamos).
    /// CONSUME: Michael (pago a prestamo desde cliente y cajero, indicadores, unicidad de numero).
    /// </summary>
    public interface ILoanReadService
    {
        Task<LoanInfoDto?> GetByIdAsync(int loanId);
        Task<LoanInfoDto?> GetByLoanNumberAsync(string loanNumber);
        Task<List<LoanInfoDto>> GetActiveByClientAsync(string clientId);
        Task<int> CountActiveAsync();
        Task<decimal> GetPendingDebtByClientAsync(string clientId);

        /// <summary>Un numero de 9 digitos no puede repetirse entre cuentas de ahorro y prestamos.</summary>
        Task<bool> LoanNumberExistsAsync(string number);

        /// <summary>
        /// Aplica el monto efectivo a la tabla de amortizacion en orden de antiguedad.
        /// Michael ya debito la cuenta de origen dentro de la misma transaccion de base de datos.
        /// </summary>
        Task ApplyPaymentAsync(int loanId, decimal effectiveAmount, CancellationToken cancellationToken = default);
    }
}
