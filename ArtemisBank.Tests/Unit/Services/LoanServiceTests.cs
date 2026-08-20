using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    /// <summary>Unit tests del servicio de prestamos (Manuel).</summary>
    public class LoanServiceTests
    {
        private static AssignLoanDto Request(decimal capital = 50000m, int term = 12, decimal rate = 12m,
            bool confirm = false)
            => new AssignLoanDto
            {
                ClientId = TestData.ClientOne,
                CapitalAmount = capital,
                TermInMonths = term,
                AnnualInterestRate = rate,
                ConfirmHighRisk = confirm,
                AssignedByUserId = TestData.AdminId
            };

        [Fact]
        public async Task Asignar_CreaPrestamoActivoConNueveDigitosYTablaCompleta()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            var created = await service.AssignAsync(Request(capital: 50000m, term: 12));

            created.LoanNumber.Should().HaveLength(9);
            created.Status.Should().Be("Activo");

            var loan = await context.Loans.Include(l => l.Installments).FirstAsync();
            loan.Installments.Should().HaveCount(12);
            loan.Status.Should().Be(LoanStatus.Activo);
        }

        [Fact]
        public async Task Asignar_DesembolsaElCapitalComoCreditoALaPrincipal()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            await service.AssignAsync(Request(capital: 75000m));

            // Se verifica que se solicito el credito externo por el capital exacto.
            builder.Transactions.Verify(t => t.RegisterExternalCreditAsync(
                It.Is<ExternalCreditRequest>(r =>
                    r.Amount == 75000m && r.Operation == TransactionOperation.DesembolsoPrestamo),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Asignar_ClienteConPrestamoActivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            await service.AssignAsync(Request());

            var second = async () => await service.AssignAsync(Request());
            await second.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task Asignar_PlazoInvalido_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            var act = async () => await service.AssignAsync(Request(term: 7));
            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task Asignar_SinCuentaPrincipal_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            // El cliente no tiene ninguna cuenta activa.
            builder.Accounts.Setup(a => a.GetActiveByClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Core.Application.Dtos.SavingsAccounts.SavingsAccountDto>());
            var service = builder.LoanService();

            var act = async () => await service.AssignAsync(Request());
            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task Asignar_ClienteDeAltoRiesgoSinConfirmar_Lanza409()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            // Solo un cliente activo ⇒ el promedio es su propia deuda; cualquier prestamo lo proyecta por encima.
            builder.Users.Setup(u => u.GetActiveClientIdsAsync())
                   .ReturnsAsync(new List<string> { TestData.ClientOne });
            var service = builder.LoanService();

            var act = async () => await service.AssignAsync(Request(confirm: false));

            var ex = await act.Should().ThrowAsync<HighRiskConflictException>();
            ex.Which.Detail.ProjectedDebt.Should().BeGreaterThan(ex.Which.Detail.AverageDebt);
        }

        [Fact]
        public async Task Asignar_AltoRiesgoConfirmado_CreaElPrestamo()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            builder.Users.Setup(u => u.GetActiveClientIdsAsync())
                   .ReturnsAsync(new List<string> { TestData.ClientOne });
            var service = builder.LoanService();

            var created = await service.AssignAsync(Request(confirm: true));

            created.Should().NotBeNull();
            (await context.Loans.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task EditarTasa_RecalculaSoloCuotasFuturasPendientes()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            var created = await service.AssignAsync(Request(capital: 60000m, term: 12, rate: 12m));
            var loan = await context.Loans.Include(l => l.Installments).FirstAsync();

            // Se marca la primera cuota como pagada para verificar que NO se recalcula.
            var first = loan.Installments.OrderBy(i => i.InstallmentNumber).First();
            first.Status = InstallmentStatus.Pagada;
            first.PendingAmount = 0m;
            var originalFirstInstallment = first.InstallmentAmount;
            await context.SaveChangesAsync();

            await service.UpdateRateAsync(loan.Id, 24m);

            var updated = await context.Loans.Include(l => l.Installments).FirstAsync();
            var paid = updated.Installments.Single(i => i.InstallmentNumber == first.InstallmentNumber);
            var future = updated.Installments.Where(i => i.InstallmentNumber > first.InstallmentNumber);

            paid.InstallmentAmount.Should().Be(originalFirstInstallment); // intacta
            updated.AnnualInterestRate.Should().Be(24m);
            future.Should().OnlyContain(i => i.PendingAmount == i.InstallmentAmount);
        }

        [Fact]
        public async Task EditarTasa_PrestamoInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            var act = async () => await service.UpdateRateAsync(9999, 15m);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RefreshOverdue_MarcaCuotasVencidasNoPagadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.LoanService();

            await service.AssignAsync(Request(term: 12));
            var loan = await context.Loans.Include(l => l.Installments).FirstAsync();

            // Se fuerza el vencimiento de las dos primeras cuotas al pasado.
            var ordered = loan.Installments.OrderBy(i => i.InstallmentNumber).ToList();
            ordered[0].DueDate = DateTime.Now.AddDays(-40);
            ordered[1].DueDate = DateTime.Now.AddDays(-10);
            await context.SaveChangesAsync();

            var changed = await service.RefreshOverdueInstallmentsAsync(DateTime.Now);

            changed.Should().Be(2);
            var reloaded = await context.Installments.Where(i => i.IsLate).CountAsync();
            reloaded.Should().Be(2);
        }
    }
}
