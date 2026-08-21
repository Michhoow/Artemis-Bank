using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class LoanServiceTests
    {
        private static CreateLoanDto Request(string clientId, decimal capital = 100000m,
            int term = 12, decimal rate = 12m, bool confirm = true) => new CreateLoanDto
            {
                ClientId = clientId,
                CapitalAmount = capital,
                TermInMonths = term,
                AnnualInterestRate = rate,

                ConfirmHighRisk = confirm
            };

        [Fact]
        public async Task CreateAsync_GeneraNumeroDeNueveDigitosComoTexto()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            loan.LoanNumber.Should().HaveLength(9);
            loan.LoanNumber.Should().MatchRegex(@"^\d{9}$");
        }

        [Fact]
        public async Task CreateAsync_ElNumeroNoColisionaConNingunaCuentaDeAhorro()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var accountNumbers = await context.SavingsAccounts.Select(a => a.AccountNumber).ToListAsync();
            accountNumbers.Should().NotContain(loan.LoanNumber);
        }

        [Fact]
        public async Task CreateAsync_GeneraLaTablaDeAmortizacionCompleta()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, term: 24), TestData.AdminId);

            loan.Installments.Should().HaveCount(24);
            loan.Installments.Should().OnlyContain(i => i.PaymentStatus == "Pendiente");

            loan.Installments.Should().OnlyContain(i => !i.IsOverdue);
        }

        [Fact]
        public async Task CreateAsync_DesembolsaElCapitalEnLaCuentaPrincipal()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var balanceBefore = principal.Balance;
            var builder = new ModuleBuilder(context);

            await builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, capital: 50000m), TestData.AdminId);

            var updated = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);
            updated.Balance.Should().Be(balanceBefore + 50000m);
        }

        [Fact]
        public async Task CreateAsync_RegistraElDesembolsoComoTransaccionDeTipoCredito()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, capital: 50000m), TestData.AdminId);

            var movement = await context.Transactions.FirstOrDefaultAsync(t =>
                t.SavingsAccountId == principal.Id &&
                t.Operation == TransactionOperation.DesembolsoPrestamo);

            movement.Should().NotBeNull();
            movement!.Type.Should().Be(TransactionType.Credito);
            movement.Amount.Should().Be(50000m);
            movement.Origin.Should().Be(loan.LoanNumber);
        }

        [Fact]
        public async Task CreateAsync_ConClienteInactivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            builder.Users.Setup(u => u.GetByIdAsync(TestData.ClientOne))
                   .ReturnsAsync(TestData.User(TestData.ClientOne, "001", isActive: false));

            var act = () => builder.LoanService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*clientes activos*");
        }

        [Fact]
        public async Task CreateAsync_ConPrestamoActivoPrevio_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var act = () => service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*ya tiene un préstamo activo*");
        }

        [Fact]
        public async Task CreateAsync_SinCuentaPrincipalActiva_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService()
                .CreateAsync(Request("cliente-sin-cuentas"), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*cuenta de ahorro principal activa*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        public async Task CreateAsync_ConMontoNoPositivo_Rechaza(decimal capital)
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, capital: capital), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*mayor que cero*");
        }

        [Fact]
        public async Task CreateAsync_ConPlazoNoPermitido_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, term: 13), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*plazo seleccionado no es válido*");
        }

        [Fact]
        public async Task CreateAsync_ConTasaNegativa_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, rate: -5m), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*no puede ser negativa*");
        }

        [Fact]
        public async Task CreateAsync_ElPrestamoNaceActivoYAlDia()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            loan.Status.Should().Be("Activo");
            loan.ClientPaymentStatus.Should().Be("Al día");
            loan.PaidInstallments.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_ConAltoRiesgoSinConfirmar_LanzaConflicto()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, confirm: false), TestData.AdminId);

            await act.Should().ThrowAsync<ConflictException>()
                     .WithMessage("*alto riesgo*");
        }

        [Fact]
        public async Task CreateAsync_ConAltoRiesgoConfirmado_CreaElPrestamo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var loan = await builder.LoanService()
                .CreateAsync(Request(TestData.ClientOne, confirm: true), TestData.AdminId);

            loan.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task EvaluateRiskAsync_NoCreaNingunPrestamo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await builder.LoanService().EvaluateRiskAsync(Request(TestData.ClientOne, confirm: false));

            (await context.Loans.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task UpdateRateAsync_RecalculaSoloLasCuotasFuturasPendientes()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            var loan = await service.CreateAsync(
                Request(TestData.ClientOne, capital: 100000m, term: 12, rate: 12m), TestData.AdminId);

            var first = await context.LoanInstallments
                .Where(i => i.LoanId == loan.Id).OrderBy(i => i.Number).FirstAsync();
            var firstAmountBefore = first.TotalAmount;
            first.PaidAmount = first.TotalAmount;
            first.Status = InstallmentStatus.Pagada;
            await context.SaveChangesAsync();

            var updated = await service.UpdateRateAsync(loan.Id, 24m, TestData.AdminId);

            var firstAfter = updated.Installments.First(i => i.Number == 1);
            firstAfter.TotalAmount.Should().Be(firstAmountBefore, "la cuota pagada no se toca");

            var futureAfter = updated.Installments.Where(i => i.Number > 1).ToList();
            futureAfter.Should().OnlyContain(i => i.TotalAmount != firstAmountBefore);
        }

        [Fact]
        public async Task UpdateRateAsync_ConservaLasFechasDeVencimientoPactadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            var loan = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);
            var datesBefore = loan.Installments.Select(i => i.DueDate).ToList();

            var updated = await service.UpdateRateAsync(loan.Id, 20m, TestData.AdminId);

            updated.Installments.Select(i => i.DueDate).Should().Equal(datesBefore);
        }

        [Fact]
        public async Task UpdateRateAsync_ActualizaLaTasaDelPrestamo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            var loan = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);
            var updated = await service.UpdateRateAsync(loan.Id, 7.5m, TestData.AdminId);

            updated.AnnualInterestRate.Should().Be(7.5m);
        }

        [Fact]
        public async Task UpdateRateAsync_ConTasaNegativa_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            var loan = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var act = () => service.UpdateRateAsync(loan.Id, -1m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task UpdateRateAsync_ConPrestamoInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService().UpdateRateAsync(9999, 10m, TestData.AdminId);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateRateAsync_SinCuotasFuturasPendientes_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.LoanService();

            var loan = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var installments = await context.LoanInstallments.Where(i => i.LoanId == loan.Id).ToListAsync();
            foreach (var installment in installments) installment.IsOverdue = true;
            await context.SaveChangesAsync();

            var act = () => service.UpdateRateAsync(loan.Id, 20m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*cuotas futuras pendientes*");
        }

        [Fact]
        public async Task AddAsync_PorLaViaGenerica_EstaBloqueado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService().AddAsync(new LoanDto());

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task DeleteAsync_PorLaViaGenerica_EstaBloqueado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var act = () => builder.LoanService().DeleteAsync(1);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }
    }
}
