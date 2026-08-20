using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Calculo de amortizacion por sistema frances (cuota fija). Servicio puro y determinista:
    /// no toca base de datos, lo que lo hace facil de probar con precision de centavos.
    /// Dueno: Manuel.
    /// </summary>
    public interface IAmortizationService
    {
        /// <summary>
        /// Cuota mensual fija: C = P*[r(1+r)^n]/[(1+r)^n-1]. Si la tasa anual es 0% ⇒ C = P/n.
        /// </summary>
        decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int termInMonths);

        /// <summary>
        /// Genera la tabla completa desde <paramref name="firstDueDate"/>. La ultima cuota ajusta
        /// el capital residual para que la suma de capitales sea exactamente el capital aprobado.
        /// </summary>
        List<AmortizationLineDto> BuildSchedule(decimal capital, decimal annualInterestRate, int termInMonths,
            DateTime firstDueDate);

        /// <summary>
        /// Recalcula el valor de un bloque de cuotas futuras sobre un capital residual y una nueva tasa,
        /// distribuido en la cantidad de cuotas restantes. Se usa al editar la tasa del prestamo.
        /// </summary>
        List<AmortizationLineDto> RecalculateFutureInstallments(decimal residualCapital, decimal newAnnualRate,
            int remainingInstallments, DateTime firstFutureDueDate, int firstInstallmentNumber);
    }
}
