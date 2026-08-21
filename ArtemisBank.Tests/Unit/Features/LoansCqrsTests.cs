using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Features.Loans.Commands;
using ArtemisBank.Core.Application.Features.Loans.Queries;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class LoansCqrsTests
    {
        private static CreateLoanCommand Comando(decimal capital = 60000m, int plazo = 12,
            decimal tasa = 12m) => new()
        {
            ClientId = TestData.ClientOne,
            CapitalAmount = capital,
            TermInMonths = plazo,
            AnnualInterestRate = tasa,
            ConfirmHighRisk = true,
            AdminUserId = TestData.AdminId
        };

        [Fact]
        public async Task CreateLoanCommandHandler_GeneraLaTablaDeAmortizacionCompleta()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());

            var prestamo = await handler.Handle(Comando(plazo: 12), CancellationToken.None);

            prestamo.Installments.Should().HaveCount(12);
            prestamo.LoanNumber.Should().MatchRegex(@"^\d{9}$");
        }

        [Fact]
        public async Task CreateLoanCommandHandler_ElCapitalDeLasCuotasSumaElCapitalAprobado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());

            var prestamo = await handler.Handle(Comando(capital: 60000m, plazo: 12),
                CancellationToken.None);

            var sumaCapital = prestamo.Installments.Sum(i => i.CapitalAmount);
            sumaCapital.Should().BeApproximately(60000m, 0.05m);
        }

        [Fact]
        public async Task CreateLoanCommandHandler_LaPrimeraCuotaVenceAlMesSiguiente()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());

            var prestamo = await handler.Handle(Comando(), CancellationToken.None);

            var primera = prestamo.Installments.OrderBy(i => i.Number).First();
            var esperada = DateTime.Now.Date.AddMonths(1);

            primera.DueDate.Date.Should().BeCloseTo(esperada, TimeSpan.FromDays(1));
        }

        [Fact]
        public async Task CreateLoanCommandHandler_ConTasaCeroLaCuotaEsCapitalEntrePlazo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());

            var prestamo = await handler.Handle(Comando(capital: 12000m, plazo: 12, tasa: 0m),
                CancellationToken.None);

            prestamo.MonthlyInstallment.Should().Be(1000m);
            prestamo.Installments.Should().OnlyContain(i => i.InterestAmount == 0m);
        }

        [Fact]
        public async Task CreateLoanCommandHandler_AcreditaElDesembolsoEnLaCuentaPrincipal()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var saldoInicial = principal.Balance;
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());

            await handler.Handle(Comando(capital: 25000m), CancellationToken.None);

            var actualizada = await context.SavingsAccounts
                .FirstAsync(a => a.AccountNumber == principal.AccountNumber);

            actualizada.Balance.Should().Be(saldoInicial + 25000m);
        }

        [Fact]
        public async Task CreateLoanCommandHandler_RechazaSegundoPrestamoActivo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateLoanCommandHandler(builder.LoanService());
            await handler.Handle(Comando(), CancellationToken.None);

            var accion = async () => await handler.Handle(Comando(capital: 10000m),
                CancellationToken.None);

            await accion.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task GetLoansQueryHandler_DevuelveElListadoPaginado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await new CreateLoanCommandHandler(builder.LoanService())
                .Handle(Comando(), CancellationToken.None);

            var handler = new GetLoansQueryHandler(builder.LoanService());

            var resultado = await handler.Handle(
                new GetLoansQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            resultado.Page.Should().Be(1);
            resultado.TotalRecords.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetLoanByIdQueryHandler_DevuelveDetalleConCuotas()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateLoanCommandHandler(builder.LoanService())
                .Handle(Comando(plazo: 6), CancellationToken.None);

            var handler = new GetLoanByIdQueryHandler(builder.LoanService());

            var detalle = await handler.Handle(new GetLoanByIdQuery { Id = creado.Id },
                CancellationToken.None);

            detalle.Should().NotBeNull();
            detalle!.Installments.Should().HaveCount(6);
        }

        [Fact]
        public async Task UpdateLoanRateCommandHandler_RecalculaSoloCuotasFuturasPendientes()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateLoanCommandHandler(builder.LoanService())
                .Handle(Comando(capital: 60000m, plazo: 12, tasa: 12m), CancellationToken.None);

            var primera = await context.LoanInstallments
                .Where(i => i.LoanId == creado.Id).OrderBy(i => i.Number).FirstAsync();
            var totalOriginalPrimera = primera.TotalAmount;
            primera.PaidAmount = primera.TotalAmount;
            await context.SaveChangesAsync();

            var handler = new UpdateLoanRateCommandHandler(builder.LoanService());

            await handler.Handle(new UpdateLoanRateCommand
            {
                LoanId = creado.Id, AnnualInterestRate = 5m, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            var primeraTrasCambio = await context.LoanInstallments
                .Where(i => i.LoanId == creado.Id).OrderBy(i => i.Number).FirstAsync();

            primeraTrasCambio.TotalAmount.Should().Be(totalOriginalPrimera);
        }

        [Fact]
        public async Task UpdateLoanRateCommandHandler_ConservaLasFechasDeVencimiento()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateLoanCommandHandler(builder.LoanService())
                .Handle(Comando(plazo: 6), CancellationToken.None);

            var fechasOriginales = await context.LoanInstallments
                .Where(i => i.LoanId == creado.Id)
                .OrderBy(i => i.Number)
                .Select(i => i.DueDate)
                .ToListAsync();

            await new UpdateLoanRateCommandHandler(builder.LoanService()).Handle(
                new UpdateLoanRateCommand { LoanId = creado.Id, AnnualInterestRate = 7m },
                CancellationToken.None);

            var fechasTrasCambio = await context.LoanInstallments
                .Where(i => i.LoanId == creado.Id)
                .OrderBy(i => i.Number)
                .Select(i => i.DueDate)
                .ToListAsync();

            fechasTrasCambio.Should().Equal(fechasOriginales);
        }

        [Theory]
        [InlineData(0, 20, "activo", false)]
        [InlineData(1, 0, "activo", false)]
        [InlineData(1, 21, "activo", false)]
        [InlineData(1, 20, "inventado", false)]
        [InlineData(1, 20, "activo", true)]
        [InlineData(2, 20, "todos", true)]
        public void GetLoansQueryValidator_ValidaParametros(int page, int pageSize,
            string status, bool esperado)
        {
            var validator = new GetLoansQueryValidator();

            var resultado = validator.Validate(new GetLoansQuery
            {
                Page = page, PageSize = pageSize, Status = status
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(6, true)]
        [InlineData(12, true)]
        [InlineData(60, true)]
        [InlineData(13, false)]
        [InlineData(0, false)]
        [InlineData(72, false)]
        public void CreateLoanCommandValidator_SoloAdmitePlazosPermitidos(int plazo, bool esperado)
        {
            var validator = new CreateLoanCommandValidator();

            var resultado = validator.Validate(new CreateLoanCommand
            {
                ClientId = "cliente-1",
                CapitalAmount = 50000m,
                TermInMonths = plazo,
                AnnualInterestRate = 10m
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("cliente-1", 50000, 10, true)]
        [InlineData("", 50000, 10, false)]
        [InlineData("cliente-1", 0, 10, false)]
        [InlineData("cliente-1", -100, 10, false)]
        [InlineData("cliente-1", 50000, 0, true)]
        [InlineData("cliente-1", 50000, -1, false)]
        public void CreateLoanCommandValidator_ValidaClienteCapitalYTasa(string clienteId,
            decimal capital, decimal tasa, bool esperado)
        {
            var validator = new CreateLoanCommandValidator();

            var resultado = validator.Validate(new CreateLoanCommand
            {
                ClientId = clienteId,
                CapitalAmount = capital,
                TermInMonths = 12,
                AnnualInterestRate = tasa
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, 8.5, true)]
        [InlineData(1, 0, true)]
        [InlineData(0, 8.5, false)]
        [InlineData(1, -2, false)]
        public void UpdateLoanRateCommandValidator_ValidaPrestamoYTasa(int prestamoId,
            decimal tasa, bool esperado)
        {
            var validator = new UpdateLoanRateCommandValidator();

            var resultado = validator.Validate(new UpdateLoanRateCommand
            {
                LoanId = prestamoId, AnnualInterestRate = tasa
            });

            resultado.IsValid.Should().Be(esperado);
        }
    }
}
