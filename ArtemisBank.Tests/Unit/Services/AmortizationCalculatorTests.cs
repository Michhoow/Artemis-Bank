using ArtemisBank.Core.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class AmortizationCalculatorTests
    {
        [Fact]
        public void CalculateMonthlyInstallment_ConTasaCero_RepartePartesIguales()
        {
            var installment = AmortizationCalculator.CalculateMonthlyInstallment(120000m, 0m, 12);

            installment.Should().Be(10000.00m);
        }

        [Fact]
        public void CalculateMonthlyInstallment_AplicaLaFormulaFrancesa()
        {
            var installment = AmortizationCalculator.CalculateMonthlyInstallment(100000m, 12m, 12);

            installment.Should().Be(8884.88m);
        }

        [Fact]
        public void CalculateMonthlyInstallment_CoincideConLaFormulaReconstruidaAparte()
        {
            const decimal capital = 250000m;
            const decimal annualRate = 18m;
            const int installments = 36;

            var actual = AmortizationCalculator.CalculateMonthlyInstallment(
                capital, annualRate, installments);

            var r = (double)annualRate / 100d / 12d;
            var factor = Math.Pow(1 + r, installments);
            var expected = Math.Round((double)capital * (r * factor) / (factor - 1), 2,
                MidpointRounding.AwayFromZero);

            ((double)actual).Should().BeApproximately(expected, 0.01);
        }

        [Fact]
        public void CalculateMonthlyInstallment_ConCantidadDeCuotasInvalida_Lanza()
        {
            var act = () => AmortizationCalculator.CalculateMonthlyInstallment(100000m, 12m, 0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void BuildPlan_GeneraUnaCuotaPorCadaMesDelPlazo()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 24, new DateTime(2026, 7, 5));

            plan.Rows.Should().HaveCount(24);
            plan.Rows.Select(r => r.Number).Should().BeInAscendingOrder();
        }

        [Fact]
        public void BuildPlan_LaPrimeraCuotaVenceElMismoDiaDelMesSiguiente()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 12, new DateTime(2025, 7, 5));

            plan.Rows[0].DueDate.Should().Be(new DateTime(2025, 8, 5));
            plan.Rows[1].DueDate.Should().Be(new DateTime(2025, 9, 5));
        }

        [Fact]
        public void BuildPlan_ConCuotaFija_TodasLasCuotasIntermediasSonIguales()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 12, DateTime.Now);

            var intermediate = plan.Rows.Take(plan.Rows.Count - 1).Select(r => r.TotalAmount).Distinct();

            intermediate.Should().ContainSingle()
                .Which.Should().Be(plan.MonthlyInstallment);
        }

        [Fact]
        public void BuildPlan_ElCapitalCierraExactamenteEnCero()
        {
            var plan = AmortizationCalculator.BuildPlan(137500.37m, 17.35m, 60, DateTime.Now);

            plan.Rows.Last().RemainingCapital.Should().Be(0m);
            plan.Rows.Sum(r => r.CapitalAmount).Should().Be(137500.37m);
        }

        [Fact]
        public void BuildPlan_LaSumaDeCapitalEInteresEsLaCuota()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 12, DateTime.Now);

            foreach (var row in plan.Rows)
                (row.CapitalAmount + row.InterestAmount).Should().Be(row.TotalAmount);
        }

        [Fact]
        public void BuildPlan_ElInteresDecreceYElCapitalCrece()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 12, DateTime.Now);

            plan.Rows.Select(r => r.InterestAmount).Should().BeInDescendingOrder();
            plan.Rows.Take(plan.Rows.Count - 1)
                .Select(r => r.CapitalAmount).Should().BeInAscendingOrder();
        }

        [Fact]
        public void BuildPlan_ConTasaCero_NoGeneraIntereses()
        {
            var plan = AmortizationCalculator.BuildPlan(120000m, 0m, 12, DateTime.Now);

            plan.Rows.Should().OnlyContain(r => r.InterestAmount == 0m);
            plan.TotalToPay.Should().Be(120000m);
        }

        [Fact]
        public void BuildPlan_TotalToPayEsLaSumaDeTodasLasCuotas()
        {
            var plan = AmortizationCalculator.BuildPlan(100000m, 12m, 12, DateTime.Now);

            plan.TotalToPay.Should().Be(plan.Rows.Sum(r => r.TotalAmount));

            plan.TotalToPay.Should().BeGreaterThan(100000m);
        }

        [Fact]
        public void BuildPlan_TodosLosImportesTienenPrecisionDeCentavos()
        {
            var plan = AmortizationCalculator.BuildPlan(99999.99m, 13.37m, 18, DateTime.Now);

            foreach (var row in plan.Rows)
            {
                decimal.Round(row.TotalAmount, 2).Should().Be(row.TotalAmount);
                decimal.Round(row.CapitalAmount, 2).Should().Be(row.CapitalAmount);
                decimal.Round(row.InterestAmount, 2).Should().Be(row.InterestAmount);
            }
        }

        [Fact]
        public void BuildPlan_ConCapitalCero_NoGeneraCuotas()
        {
            var plan = AmortizationCalculator.BuildPlan(0m, 12m, 12, DateTime.Now);

            plan.Rows.Should().BeEmpty();
        }

        [Fact]
        public void AddMonthsSafe_ConservaElDiaCuandoElMesDestinoNoLoTiene()
        {
            var due = AmortizationCalculator.AddMonthsSafe(new DateTime(2026, 1, 31), 1);

            due.Should().Be(new DateTime(2026, 2, 28));
        }

        [Fact]
        public void AddMonthsSafe_EnAnioBisiestoUsaElDia29()
        {
            var due = AmortizationCalculator.AddMonthsSafe(new DateTime(2028, 1, 31), 1);

            due.Should().Be(new DateTime(2028, 2, 29));
        }

        [Theory]
        [InlineData(6)]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(36)]
        [InlineData(60)]
        public void IsAllowedTerm_AceptaLosPlazosDelDocumentoFuncional(int term)
            => AmortizationCalculator.IsAllowedTerm(term).Should().BeTrue();

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(13)]
        [InlineData(72)]
        [InlineData(-12)]
        public void IsAllowedTerm_RechazaCualquierOtroPlazo(int term)
            => AmortizationCalculator.IsAllowedTerm(term).Should().BeFalse();
    }
}
