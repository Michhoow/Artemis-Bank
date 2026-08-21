using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class BeneficiaryAndIndicatorsTests
    {
        [Fact]
        public async Task Beneficiario_CuentaAjenaActiva_SeRegistra()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, _, other) = await TestData.SeedAsync(context);

            await builder.BeneficiaryService().AddAsync(TestData.ClientOne, other.AccountNumber);

            (await context.Beneficiaries.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Beneficiario_CuentaPropia_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var act = async () => await builder.BeneficiaryService()
                .AddAsync(TestData.ClientOne, principal.AccountNumber);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.OwnAccountAsBeneficiary);
        }

        [Fact]
        public async Task Beneficiario_CuentaCancelada_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, _, other) = await TestData.SeedAsync(context);

            other.Status = AccountStatus.Cancelada;
            await context.SaveChangesAsync();

            var act = async () => await builder.BeneficiaryService()
                .AddAsync(TestData.ClientOne, other.AccountNumber);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.CancelledAccountAsBeneficiary);
        }

        [Fact]
        public async Task Beneficiario_Duplicado_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, _, other) = await TestData.SeedAsync(context);
            var service = builder.BeneficiaryService();

            await service.AddAsync(TestData.ClientOne, other.AccountNumber);
            var act = async () => await service.AddAsync(TestData.ClientOne, other.AccountNumber);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.BeneficiaryAlreadyRegistered);
        }

        [Fact]
        public async Task Beneficiario_CuentaInexistente_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);

            var act = async () => await builder.BeneficiaryService().AddAsync(TestData.ClientOne, "888888888");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.InvalidAccountNumber);
        }

        [Fact]
        public async Task Beneficiario_DeOtroCliente_NoSePuedeEliminar()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, _, other) = await TestData.SeedAsync(context);
            var service = builder.BeneficiaryService();

            await service.AddAsync(TestData.ClientOne, other.AccountNumber);
            var created = await context.Beneficiaries.SingleAsync();

            var act = async () => await service.DeleteAsync(TestData.ClientTwo, created.Id);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Beneficiario_EliminarNoAfectaLaCuentaNiSuHistorial()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);
            var service = builder.BeneficiaryService();

            await service.AddAsync(TestData.ClientOne, other.AccountNumber);

            await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 100m,
                Operation = TransactionOperation.TransaccionBeneficiario,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            var created = await context.Beneficiaries.SingleAsync();
            await service.DeleteAsync(TestData.ClientOne, created.Id);

            (await context.Beneficiaries.CountAsync()).Should().Be(0);
            (await context.SavingsAccounts.CountAsync(a => a.Id == other.Id)).Should().Be(1);
            (await context.Transactions.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task IndicadoresAdmin_TransferenciaCuentaComoUnaSolaTransaccion()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);

            await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 100m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            var indicators = await builder.AdminHomeService().GetIndicatorsAsync();

            (await context.Transactions.CountAsync()).Should().Be(2);
            indicators.TotalHistoricTransactions.Should().Be(1);
            indicators.TodayTransactions.Should().Be(1);
            indicators.TotalHistoricPayments.Should().Be(0);
        }

        [Fact]
        public async Task IndicadoresAdmin_SinClientesActivos_DeudaPromedioEsCero()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            builder.Users.Setup(u => u.GetActiveClientIdsAsync()).ReturnsAsync(new List<string>());

            var indicators = await builder.AdminHomeService().GetIndicatorsAsync();

            indicators.AverageDebtPerClient.Should().Be(0m);
        }

        [Fact]
        public async Task IndicadoresAdmin_DeudaPromedioSeCalculaSobreClientesActivos()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);

            builder.Users.Setup(u => u.GetActiveClientIdsAsync())
                   .ReturnsAsync(new List<string> { TestData.ClientOne, TestData.ClientTwo });
            builder.Loans.Setup(l => l.GetPendingDebtByClientAsync(TestData.ClientOne)).ReturnsAsync(1000m);
            builder.Cards.Setup(c => c.GetDebtByClientAsync(TestData.ClientOne)).ReturnsAsync(500m);
            builder.Loans.Setup(l => l.GetPendingDebtByClientAsync(TestData.ClientTwo)).ReturnsAsync(0m);
            builder.Cards.Setup(c => c.GetDebtByClientAsync(TestData.ClientTwo)).ReturnsAsync(500m);

            var indicators = await builder.AdminHomeService().GetIndicatorsAsync();

            indicators.AverageDebtPerClient.Should().Be(1000m);
        }

        [Fact]
        public async Task IndicadoresCajero_SoloCuentanLasOperacionesDelCajeroAutenticado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);
            var service = builder.TransactionService();

            await service.DepositAsync(new DepositRequest
            {
                TargetAccountNumber = principal.AccountNumber,
                Amount = 100m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            await service.DepositAsync(new DepositRequest
            {
                TargetAccountNumber = principal.AccountNumber,
                Amount = 200m,
                Actor = OperationActor.Of("otro-cajero", Roles.Cajero)
            });

            await service.WithdrawAsync(new WithdrawalRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                Amount = 50m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            var indicators = await builder.CashierHomeService().GetIndicatorsAsync(TestData.CashierId);

            indicators.TodayTransactions.Should().Be(2);
            indicators.TodayDeposits.Should().Be(1);
            indicators.TodayWithdrawals.Should().Be(1);
            indicators.TodayPayments.Should().Be(0);
        }
    }
}
