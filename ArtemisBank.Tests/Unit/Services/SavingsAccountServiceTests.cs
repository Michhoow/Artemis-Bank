using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    /// <summary>Unit tests de servicios de cuentas y balances.</summary>
    public class SavingsAccountServiceTests
    {
        [Fact]
        public async Task CreateSecondary_GeneraNumeroDeNueveDigitosYCuentaSecundariaActiva()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);
            var service = builder.SavingsAccountService();

            var created = await service.CreateSecondaryAsync(TestData.ClientOne, 0m, TestData.AdminId);

            created.AccountNumber.Should().HaveLength(9);
            created.AccountNumber.Should().MatchRegex("^[0-9]{9}$");
            created.Type.Should().Be("Secundaria");
            created.Status.Should().Be("Activa");
        }

        [Fact]
        public async Task CreateSecondary_ConBalanceInicial_RegistraTransaccionDeCredito()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);
            var service = builder.SavingsAccountService();

            var created = await service.CreateSecondaryAsync(TestData.ClientOne, 1500m, TestData.AdminId);

            var account = await context.SavingsAccounts
                .FirstAsync(a => a.AccountNumber == created.AccountNumber);
            var movements = await context.Transactions
                .Where(t => t.SavingsAccountId == account.Id).ToListAsync();

            account.Balance.Should().Be(1500m);
            movements.Should().ContainSingle();
            movements[0].Type.Should().Be(TransactionType.Credito);
            movements[0].Operation.Should().Be(TransactionOperation.BalanceInicial);
        }

        [Fact]
        public async Task CreateSecondary_SinBalance_NoRegistraTransaccion()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);
            var service = builder.SavingsAccountService();

            var created = await service.CreateSecondaryAsync(TestData.ClientOne, 0m, TestData.AdminId);

            var account = await context.SavingsAccounts
                .FirstAsync(a => a.AccountNumber == created.AccountNumber);
            (await context.Transactions.CountAsync(t => t.SavingsAccountId == account.Id)).Should().Be(0);
        }

        [Fact]
        public async Task CreateSecondary_ClienteSinCuentaPrincipalActiva_Falla()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var service = builder.SavingsAccountService();

            var act = async () => await service.CreateSecondaryAsync("cliente-sin-principal", 0m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.ClientNeedsPrincipalAccount);
        }

        [Fact]
        public async Task CreateSecondary_ClienteInactivo_Falla()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);

            builder.Users.Setup(u => u.GetByIdAsync(TestData.ClientOne))
                   .ReturnsAsync(TestData.User(TestData.ClientOne, "00100000001", isActive: false));

            var service = builder.SavingsAccountService();
            var act = async () => await service.CreateSecondaryAsync(TestData.ClientOne, 0m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage(AppMessages.OnlyActiveClients);
        }

        [Fact]
        public async Task CreateSecondary_BalanceNegativo_Falla()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);
            var service = builder.SavingsAccountService();

            var act = async () => await service.CreateSecondaryAsync(TestData.ClientOne, -1m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.NegativeInitialBalance);
        }

        [Fact]
        public async Task Cancelar_CuentaSecundariaConBalance_TrasladaFondosYRegistraCruzado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, secondary, _) = await TestData.SeedAsync(context);
            var service = builder.SavingsAccountService();

            await service.CancelSecondaryAsync(secondary.AccountNumber, TestData.AdminId);

            var updatedSecondary = await context.SavingsAccounts.FirstAsync(a => a.Id == secondary.Id);
            var updatedPrincipal = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);

            updatedSecondary.Status.Should().Be(AccountStatus.Cancelada);
            updatedSecondary.Balance.Should().Be(0m);
            updatedPrincipal.Balance.Should().Be(12500m);

            var movements = await context.Transactions
                .Where(t => t.Operation == TransactionOperation.CancelacionCuenta).ToListAsync();

            movements.Should().HaveCount(2);
            movements.Select(m => m.OperationReference).Distinct().Should().ContainSingle();
            movements.Should().ContainSingle(m => m.Type == TransactionType.Debito
                                                  && m.SavingsAccountId == secondary.Id);
            movements.Should().ContainSingle(m => m.Type == TransactionType.Credito
                                                  && m.SavingsAccountId == principal.Id);
        }

        [Fact]
        public async Task Cancelar_CuentaSecundariaSinBalance_NoGeneraMovimientos()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, secondary, _) = await TestData.SeedAsync(context);

            secondary.Balance = 0m;
            await context.SaveChangesAsync();

            await builder.SavingsAccountService().CancelSecondaryAsync(secondary.AccountNumber, TestData.AdminId);

            (await context.Transactions.CountAsync()).Should().Be(0);
            (await context.SavingsAccounts.FirstAsync(a => a.Id == secondary.Id)).Status
                .Should().Be(AccountStatus.Cancelada);
        }

        [Fact]
        public async Task Cancelar_CuentaPrincipal_Falla()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var act = async () => await builder.SavingsAccountService()
                .CancelSecondaryAsync(principal.AccountNumber, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage(AppMessages.PrincipalCannotBeCancelled);
        }

        [Fact]
        public async Task Cancelar_CuentaInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);

            var act = async () => await builder.SavingsAccountService()
                .CancelSecondaryAsync("999999999", TestData.AdminId);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetActiveByClient_PrincipalPrimeroYSecundariasPorBalanceDescendente()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);

            context.SavingsAccounts.Add(TestData.Account("100000009", TestData.ClientOne, 9000m));
            await context.SaveChangesAsync();

            var accounts = await builder.SavingsAccountService().GetActiveByClientAsync(TestData.ClientOne);

            accounts.Should().HaveCount(3);
            accounts[0].Type.Should().Be("Principal");
            accounts[1].Balance.Should().Be(9000m);
            accounts[2].Balance.Should().Be(2500m);
        }

        [Fact]
        public async Task Buscar_PorCedulaInexistente_DevuelveMensajeDelDocumentoFuncional()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            builder.Users.Setup(u => u.GetByIdentificationAsync(It.IsAny<string>()))
                   .ReturnsAsync((UserInfoDto?)null);

            var (result, message) = await builder.SavingsAccountService()
                .SearchAsync(new SavingsAccountFilterDto
                {
                    Identification = "99999999999"
                });

            result.TotalRecords.Should().Be(0);
            message.Should().Be(AppMessages.ClientNotFoundByIdentification);
        }

        [Fact]
        public async Task Buscar_PageSizeMayorAlMaximo_SeLimitaAVeinte()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);

            var (result, _) = await builder.SavingsAccountService()
                .SearchAsync(new SavingsAccountFilterDto { PageSize = 500 });

            result.PageSize.Should().Be(20);
        }
    }
}
