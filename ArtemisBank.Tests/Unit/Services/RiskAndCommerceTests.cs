using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class RiskAssessmentServiceTests
    {
        [Fact]
        public async Task GetAverageActiveClientDebtAsync_SinClientesActivos_EsCero()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            builder.Users.Setup(u => u.GetActiveClientIdsAsync()).ReturnsAsync(new List<string>());

            var average = await builder.RiskService().GetAverageActiveClientDebtAsync();

            average.Should().Be(0m);
        }

        [Fact]
        public async Task GetAverageActiveClientDebtAsync_DivideEntreLosClientesActivos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            context.CreditCards.Add(new CreditCard
            {
                CardNumber = "4000000000000001",
                ClientId = TestData.ClientOne,
                CreditLimit = 50000m,
                Debt = 30000m,
                CvcHash = "hash",
                CvcSalt = "salt",
                ExpirationMonth = DateTime.Now.Month,
                ExpirationYear = DateTime.Now.Year + 3,
                Status = CardStatus.Activa,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();

            var average = await builder.RiskService().GetAverageActiveClientDebtAsync();

            average.Should().Be(15000m);
        }

        [Fact]
        public async Task EvaluateAsync_ConDeudaActualSobreElPromedio_MarcaRiesgoActual()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            context.CreditCards.Add(new CreditCard
            {
                CardNumber = "4000000000000002",
                ClientId = TestData.ClientOne,
                CreditLimit = 100000m,
                Debt = 40000m,
                CvcHash = "hash",
                CvcSalt = "salt",
                ExpirationMonth = DateTime.Now.Month,
                ExpirationYear = DateTime.Now.Year + 3,
                Status = CardStatus.Activa,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();

            var evaluation = await builder.RiskService().EvaluateAsync(TestData.ClientOne, 0m);

            evaluation.IsHighRisk.Should().BeTrue();
            evaluation.RiskType.Should().Be("CurrentHighRisk");
        }

        [Fact]
        public async Task EvaluateAsync_SoloAlProyectar_MarcaRiesgoProyectado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            foreach (var (clientId, number) in new[]
                     {
                         (TestData.ClientOne, "4000000000000003"),
                         (TestData.ClientTwo, "4000000000000004")
                     })
            {
                context.CreditCards.Add(new CreditCard
                {
                    CardNumber = number,
                    ClientId = clientId,
                    CreditLimit = 100000m,
                    Debt = 20000m,
                    CvcHash = "hash",
                    CvcSalt = "salt",
                    ExpirationMonth = DateTime.Now.Month,
                    ExpirationYear = DateTime.Now.Year + 3,
                    Status = CardStatus.Activa,
                    CreatedAt = DateTime.Now
                });
            }
            await context.SaveChangesAsync();

            var evaluation = await builder.RiskService().EvaluateAsync(TestData.ClientOne, 50000m);

            evaluation.IsHighRisk.Should().BeTrue();
            evaluation.RiskType.Should().Be("ProjectedHighRisk");
        }

        [Fact]
        public async Task EvaluateAsync_LasTarjetasCanceladasNoCuentanComoDeuda()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            context.CreditCards.Add(new CreditCard
            {
                CardNumber = "4000000000000005",
                ClientId = TestData.ClientOne,
                CreditLimit = 100000m,
                Debt = 90000m,
                CvcHash = "hash",
                CvcSalt = "salt",
                ExpirationMonth = DateTime.Now.Month,
                ExpirationYear = DateTime.Now.Year + 3,

                Status = CardStatus.Cancelada,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();

            var debt = await builder.RiskService().GetClientDebtAsync(TestData.ClientOne);

            debt.Should().Be(0m);
        }
    }

    public class CommerceServiceTests
    {
        private static SaveCommerceDto Request(string rnc = "130123456",
            string email = "comercio@artemisbank.do") => new SaveCommerceDto
            {
                Name = "Supermercado Hermes",
                Rnc = rnc,
                Email = email,
                PhoneNumber = "8095551234"
            };

        [Fact]
        public async Task CreateAsync_ElComercioNaceActivoYSinUsuario()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var commerce = await builder.CommerceService().CreateAsync(Request(), TestData.AdminId);

            commerce.IsActive.Should().BeTrue();
            commerce.UserId.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_ConRncDuplicado_LanzaConflicto()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            var service = builder.CommerceService();

            await service.CreateAsync(Request(), TestData.AdminId);

            var act = () => service.CreateAsync(
                Request(email: "otro@artemisbank.do"), TestData.AdminId);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task CreateAsync_ConCorreoDuplicado_LanzaConflicto()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            var service = builder.CommerceService();

            await service.CreateAsync(Request(), TestData.AdminId);

            var act = () => service.CreateAsync(Request(rnc: "130999999"), TestData.AdminId);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task UpdateAsync_NoModificaElEstadoDelComercio()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            var service = builder.CommerceService();

            var created = await service.CreateAsync(Request(), TestData.AdminId);
            await service.SetStatusAsync(created.Id, false, TestData.AdminId);

            var updated = await service.UpdateAsync(created.Id, Request());

            updated.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task SetStatusAsync_AlDesactivar_InactivaLosUsuariosAsociados()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            var service = builder.CommerceService();

            var created = await service.CreateAsync(Request(), TestData.AdminId);

            var entity = await context.Commerces.FirstAsync();
            entity.UserId = "usuario-comercio";
            await context.SaveChangesAsync();

            await service.SetStatusAsync(created.Id, false, TestData.AdminId);

            builder.UserManagement.Verify(u => u.DeactivateUsersOfCommerceAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_AlReactivar_NoReactivaLosUsuarios()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);
            var service = builder.CommerceService();

            var created = await service.CreateAsync(Request(), TestData.AdminId);

            var entity = await context.Commerces.FirstAsync();
            entity.UserId = "usuario-comercio";
            entity.IsActive = false;
            await context.SaveChangesAsync();

            var result = await service.SetStatusAsync(created.Id, true, TestData.AdminId);

            result.IsActive.Should().BeTrue();

            builder.UserManagement.Verify(u => u.DeactivateUsersOfCommerceAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SetStatusAsync_ConComercioInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var act = () => builder.CommerceService().SetStatusAsync(9999, false, TestData.AdminId);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_EstaBloqueado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ModuleBuilder(context);

            var act = () => builder.CommerceService().DeleteAsync(1);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }
    }

    public class OverdueInstallmentProcessorTests
    {
        private static OverdueInstallmentProcessor Build(Infrastructure.Persistence.Contexts.ArtemisDbContext context)
            => new OverdueInstallmentProcessor(
                new LoanInstallmentRepository(context),
                NullLogger<OverdueInstallmentProcessor>.Instance);

        private static async Task<Loan> SeedLoanAsync(
            Infrastructure.Persistence.Contexts.ArtemisDbContext context,
            params (DateTime Due, decimal Total, decimal Paid)[] installments)
        {
            var loan = new Loan
            {
                LoanNumber = "500000001",
                ClientId = TestData.ClientOne,
                ApprovedCapital = 100000m,
                AnnualInterestRate = 12m,
                TermInMonths = installments.Length,
                MonthlyInstallment = 10000m,
                Status = LoanStatus.Activo,
                CreatedAt = DateTime.Now
            };
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var number = 1;
            foreach (var (due, total, paid) in installments)
            {
                context.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = loan.Id,
                    Number = number++,
                    DueDate = due,
                    TotalAmount = total,
                    CapitalAmount = total,
                    InterestAmount = 0m,
                    PaidAmount = paid,
                    Status = paid >= total ? InstallmentStatus.Pagada
                        : paid > 0 ? InstallmentStatus.ParcialmentePagada
                        : InstallmentStatus.Pendiente,
                    IsOverdue = false
                });
            }
            await context.SaveChangesAsync();
            return loan;
        }

        [Fact]
        public async Task RunAsync_MarcaLasCuotasVencidasYNoSaldadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context,
                (today.AddMonths(-2), 10000m, 0m),
                (today.AddMonths(-1), 10000m, 0m),
                (today.AddMonths(1), 10000m, 0m));

            var result = await Build(context).RunAsync(today);

            result.InstallmentsMarkedOverdue.Should().Be(2);

            var overdue = await context.LoanInstallments.Where(i => i.IsOverdue).ToListAsync();
            overdue.Should().HaveCount(2);
            overdue.Should().OnlyContain(i => i.DueDate < today);
        }

        [Fact]
        public async Task RunAsync_NoMarcaLasCuotasFuturas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context,
                (today.AddMonths(1), 10000m, 0m),
                (today.AddMonths(2), 10000m, 0m));

            var result = await Build(context).RunAsync(today);

            result.InstallmentsMarkedOverdue.Should().Be(0);
            (await context.LoanInstallments.CountAsync(i => i.IsOverdue)).Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_NoMarcaUnaCuotaVencidaQueYaEstaSaldada()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context, (today.AddMonths(-1), 10000m, 10000m));

            var result = await Build(context).RunAsync(today);

            result.InstallmentsMarkedOverdue.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_MarcaUnaCuotaVencidaConPagoParcial()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context, (today.AddMonths(-1), 10000m, 4000m));

            var result = await Build(context).RunAsync(today);

            result.InstallmentsMarkedOverdue.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_ApagaElAtrasoDeUnaCuotaQueSeSaldoDespues()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context, (today.AddMonths(-1), 10000m, 10000m));

            var installment = await context.LoanInstallments.FirstAsync();
            installment.IsOverdue = true;
            await context.SaveChangesAsync();

            var result = await Build(context).RunAsync(today);

            result.InstallmentsCleared.Should().Be(1);
            (await context.LoanInstallments.FirstAsync()).IsOverdue.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsync_EsIdempotente()
        {
            using var context = TestContextFactory.CreateInMemory();
            var today = new DateTime(2026, 6, 15);

            await SeedLoanAsync(context, (today.AddMonths(-1), 10000m, 0m));

            var processor = Build(context);
            await processor.RunAsync(today);
            var second = await processor.RunAsync(today);

            second.InstallmentsMarkedOverdue.Should().Be(0);
            (await context.LoanInstallments.CountAsync(i => i.IsOverdue)).Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_SinCuotas_NoFalla()
        {
            using var context = TestContextFactory.CreateInMemory();

            var result = await Build(context).RunAsync(DateTime.Now);

            result.InstallmentsMarkedOverdue.Should().Be(0);
            result.InstallmentsCleared.Should().Be(0);
        }
    }
}
