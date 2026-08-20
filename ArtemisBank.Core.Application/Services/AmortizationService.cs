using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    /// <summary>
    /// Sistema frances de amortizacion. Servicio puro (sin dependencias de infraestructura).
    ///
    /// Formula de la cuota fija:
    ///   C = P * [ r (1 + r)^n ] / [ (1 + r)^n - 1 ]
    ///   r = (tasa anual / 100) / 12
    ///   Si la tasa anual es 0% ⇒ C = P / n
    ///
    /// Todos los valores monetarios se redondean a 2 decimales con la politica unica del sistema (Money).
    /// La ultima cuota absorbe el residuo de redondeo para que la suma de capitales cuadre con P.
    /// </summary>
    public class AmortizationService : IAmortizationService
    {
        public decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int termInMonths)
        {
            if (termInMonths <= 0) return 0m;
            if (capital <= 0m) return 0m;

            if (annualInterestRate <= 0m)
                return Money.Round(capital / termInMonths);

            var r = MonthlyRate(annualInterestRate);
            var pow = Pow(1m + r, termInMonths);
            var installment = capital * (r * pow) / (pow - 1m);

            return Money.Round(installment);
        }

        public List<AmortizationLineDto> BuildSchedule(decimal capital, decimal annualInterestRate,
            int termInMonths, DateTime firstDueDate)
        {
            var lines = new List<AmortizationLineDto>();
            if (termInMonths <= 0 || capital <= 0m) return lines;

            var installment = CalculateMonthlyInstallment(capital, annualInterestRate, termInMonths);
            var monthlyRate = annualInterestRate <= 0m ? 0m : MonthlyRate(annualInterestRate);

            var remainingCapital = capital;

            for (var number = 1; number <= termInMonths; number++)
            {
                var interest = Money.Round(remainingCapital * monthlyRate);
                var capitalPortion = Money.Round(installment - interest);

                // Ultima cuota: liquida el capital residual exacto para evitar centavos colgados.
                if (number == termInMonths)
                {
                    capitalPortion = Money.Round(remainingCapital);
                    installment = Money.Round(capitalPortion + interest);
                }

                remainingCapital = Money.Round(remainingCapital - capitalPortion);

                lines.Add(new AmortizationLineDto
                {
                    InstallmentNumber = number,
                    DueDate = firstDueDate.AddMonths(number - 1),
                    InstallmentAmount = number == termInMonths ? installment
                        : CalculateMonthlyInstallment(capital, annualInterestRate, termInMonths),
                    InterestAmount = interest,
                    CapitalAmount = capitalPortion
                });
            }

            return lines;
        }

        public List<AmortizationLineDto> RecalculateFutureInstallments(decimal residualCapital, decimal newAnnualRate,
            int remainingInstallments, DateTime firstFutureDueDate, int firstInstallmentNumber)
        {
            var lines = new List<AmortizationLineDto>();
            if (remainingInstallments <= 0 || residualCapital <= 0m) return lines;

            var installment = CalculateMonthlyInstallment(residualCapital, newAnnualRate, remainingInstallments);
            var monthlyRate = newAnnualRate <= 0m ? 0m : MonthlyRate(newAnnualRate);

            var remainingCapital = residualCapital;

            for (var i = 0; i < remainingInstallments; i++)
            {
                var interest = Money.Round(remainingCapital * monthlyRate);
                var capitalPortion = Money.Round(installment - interest);

                if (i == remainingInstallments - 1)
                {
                    capitalPortion = Money.Round(remainingCapital);
                    installment = Money.Round(capitalPortion + interest);
                }

                remainingCapital = Money.Round(remainingCapital - capitalPortion);

                lines.Add(new AmortizationLineDto
                {
                    InstallmentNumber = firstInstallmentNumber + i,
                    DueDate = firstFutureDueDate.AddMonths(i),
                    InstallmentAmount = i == remainingInstallments - 1 ? installment
                        : CalculateMonthlyInstallment(residualCapital, newAnnualRate, remainingInstallments),
                    InterestAmount = interest,
                    CapitalAmount = capitalPortion
                });
            }

            return lines;
        }

        // ------------------------------------------------------------------ helpers

        private static decimal MonthlyRate(decimal annualInterestRate) => annualInterestRate / 100m / 12m;

        /// <summary>Potencia entera en decimal para conservar precision (evita el doble de Math.Pow).</summary>
        private static decimal Pow(decimal baseValue, int exponent)
        {
            decimal result = 1m;
            for (var i = 0; i < exponent; i++) result *= baseValue;
            return result;
        }
    }
}
