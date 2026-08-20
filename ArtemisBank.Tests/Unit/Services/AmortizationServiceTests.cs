using ArtemisBank.Core.Application.Services;
using FluentAssertions;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests del calculo de amortizacion frances (Manuel). Servicio puro: sin base de datos ni mocks.
    /// </summary>
    public class AmortizationServiceTests
    {
        private readonly AmortizationService _service = new();

        [Fact]
        public void CuotaMensual_ConTasa_UsaFormulaFrancesa()
        {
            // P=100000, tasa anual 12% ⇒ r mensual = 0.01, n=12.
            // C = 100000 * 0.01 * 1.01^12 / (1.01^12 - 1) ≈ 8884.88
            var cuota = _service.CalculateMonthlyInstallment(100000m, 12m, 12);

            cuota.Should().BeApproximately(8884.88m, 0.05m);
        }

        [Fact]
        public void CuotaMensual_TasaCero_EsCapitalEntrePlazo()
        {
            var cuota = _service.CalculateMonthlyInstallment(60000m, 0m, 12);

            cuota.Should().Be(5000m);
        }

        [Fact]
        public void Tabla_SumaDeCapitales_IgualAlCapitalAprobado()
        {
            var capital = 100000m;
            var tabla = _service.BuildSchedule(capital, 12m, 12, new DateTime(2026, 1, 15));

            tabla.Should().HaveCount(12);
            tabla.Sum(l => l.CapitalAmount).Should().Be(capital);
        }

        [Fact]
        public void Tabla_TasaCero_RepartoUniformeDeCapital()
        {
            var tabla = _service.BuildSchedule(60000m, 0m, 12, new DateTime(2026, 1, 15));

            tabla.Should().OnlyContain(l => l.InterestAmount == 0m);
            tabla.Sum(l => l.CapitalAmount).Should().Be(60000m);
            tabla[0].InstallmentAmount.Should().Be(5000m);
        }

        [Fact]
        public void Tabla_PrimeraCuota_VenceUnMesDespuesDeLaFechaInicial()
        {
            var first = new DateTime(2026, 1, 31);
            var tabla = _service.BuildSchedule(50000m, 10m, 6, first);

            tabla[0].DueDate.Should().Be(first);
            tabla[1].DueDate.Should().Be(first.AddMonths(1));
            tabla[5].DueDate.Should().Be(first.AddMonths(5));
        }

        [Fact]
        public void Tabla_InteresDecreciente_CapitalCreciente()
        {
            var tabla = _service.BuildSchedule(100000m, 12m, 12, new DateTime(2026, 1, 15));

            // En el sistema frances el interes baja y el capital sube cuota a cuota.
            tabla.First().InterestAmount.Should().BeGreaterThan(tabla.Last().InterestAmount);
            tabla.First().CapitalAmount.Should().BeLessThan(tabla.Last().CapitalAmount);
        }

        [Fact]
        public void Recalculo_FuturasCuotas_LiquidaElCapitalResidual()
        {
            var residual = 40000m;
            var lineas = _service.RecalculateFutureInstallments(residual, 18m, 6,
                new DateTime(2026, 6, 15), 7);

            lineas.Should().HaveCount(6);
            lineas.First().InstallmentNumber.Should().Be(7);
            lineas.Sum(l => l.CapitalAmount).Should().Be(residual);
        }

        [Fact]
        public void Recalculo_SinCuotas_DevuelveListaVacia()
        {
            var lineas = _service.RecalculateFutureInstallments(40000m, 18m, 0,
                new DateTime(2026, 6, 15), 7);

            lineas.Should().BeEmpty();
        }
    }
}
