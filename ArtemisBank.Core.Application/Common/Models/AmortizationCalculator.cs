namespace ArtemisBank.Core.Application.Common.Models
{
    public class AmortizationRow
    {
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal InterestAmount { get; set; }

        public decimal RemainingCapital { get; set; }
    }

    public class AmortizationPlan
    {
        public decimal MonthlyInstallment { get; set; }
        public decimal TotalToPay { get; set; }
        public List<AmortizationRow> Rows { get; set; } = new();
    }

    public static class AmortizationCalculator
    {
        public static readonly int[] AllowedTerms = { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

        public static bool IsAllowedTerm(int termInMonths) => AllowedTerms.Contains(termInMonths);

        public static decimal MonthlyRate(decimal annualInterestRate)
            => annualInterestRate / 100m / 12m;

        public static decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate,
            int numberOfInstallments)
        {
            if (numberOfInstallments <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfInstallments),
                    "La cantidad de cuotas debe ser mayor que cero.");

            if (capital <= 0m) return 0m;

            var r = MonthlyRate(annualInterestRate);

            if (r == 0m) return Money.Round(capital / numberOfInstallments);

            var factor = PowDecimal(1m + r, numberOfInstallments);
            var installment = capital * (r * factor) / (factor - 1m);

            return Money.Round(installment);
        }

        public static AmortizationPlan BuildPlan(decimal capital, decimal annualInterestRate,
            int numberOfInstallments, DateTime startDate)
        {
            var plan = new AmortizationPlan();

            if (numberOfInstallments <= 0 || capital <= 0m) return plan;

            var r = MonthlyRate(annualInterestRate);
            var installment = CalculateMonthlyInstallment(capital, annualInterestRate, numberOfInstallments);

            plan.MonthlyInstallment = installment;

            var balance = Money.Round(capital);

            for (var k = 1; k <= numberOfInstallments; k++)
            {
                var interest = Money.Round(balance * r);
                var principal = Money.Round(installment - interest);
                var total = Money.Round(interest + principal);

                if (k == numberOfInstallments || principal > balance)
                {
                    principal = balance;
                    total = Money.Round(interest + principal);
                }

                balance = Money.Round(balance - principal);

                plan.Rows.Add(new AmortizationRow
                {
                    Number = k,
                    DueDate = AddMonthsSafe(startDate, k),
                    TotalAmount = total,
                    CapitalAmount = principal,
                    InterestAmount = interest,
                    RemainingCapital = balance
                });

                if (balance <= 0m && k < numberOfInstallments)
                {
                    break;
                }
            }

            plan.TotalToPay = Money.Round(plan.Rows.Sum(row => row.TotalAmount));
            return plan;
        }

        public static DateTime AddMonthsSafe(DateTime origin, int months)
        {
            var target = origin.AddMonths(months);
            var daysInTargetMonth = DateTime.DaysInMonth(target.Year, target.Month);
            var day = Math.Min(origin.Day, daysInTargetMonth);
            return new DateTime(target.Year, target.Month, day, 0, 0, 0);
        }

        private static decimal PowDecimal(decimal value, int exponent)
        {
            if (exponent == 0) return 1m;

            var result = 1m;
            var baseValue = value;
            var e = exponent;

            while (e > 0)
            {
                if ((e & 1) == 1) result *= baseValue;
                e >>= 1;
                if (e > 0) baseValue *= baseValue;
            }

            return result;
        }
    }
}
