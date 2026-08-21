using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Features.SavingsAccounts.Commands;
using ArtemisBank.Core.Application.Features.SavingsAccounts.Queries;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class SavingsAccountsCqrsTests
    {
        [Theory]
        [InlineData(0, 20, "activa", "todas", false)]
        [InlineData(1, 0, "activa", "todas", false)]
        [InlineData(1, 50, "activa", "todas", false)]
        [InlineData(1, 20, "inventado", "todas", false)]
        [InlineData(1, 20, "activa", "inventado", false)]
        [InlineData(1, 20, "activa", "todas", true)]
        [InlineData(2, 20, "cancelada", "secundaria", true)]
        public void GetSavingsAccountsQueryValidator_ValidaParametros(int page, int pageSize,
            string status, string type, bool expected)
        {
            var validator = new GetSavingsAccountsQueryValidator();

            var result = validator.Validate(new GetSavingsAccountsQuery
            {
                Page = page, PageSize = pageSize, Status = status, Type = type
            });

            result.IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("123456789", true)]
        [InlineData("12345678", false)]
        [InlineData("1234567890", false)]
        [InlineData("12345678A", false)]
        [InlineData("", false)]
        public void GetAccountTransactionsQueryValidator_ExigeNueveDigitos(string accountNumber, bool expected)
        {
            var validator = new GetAccountTransactionsQueryValidator();
            validator.Validate(new GetAccountTransactionsQuery { AccountNumber = accountNumber })
                     .IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("cliente-1", 0, true)]
        [InlineData("cliente-1", 5000, true)]
        [InlineData("cliente-1", -1, false)]
        [InlineData("", 100, false)]
        public void CreateSecondaryCommandValidator_ValidaClienteYBalance(string clientId,
            decimal balance, bool expected)
        {
            var validator = new CreateSecondarySavingsAccountCommandValidator();
            validator.Validate(new CreateSecondarySavingsAccountCommand
            {
                ClientId = clientId, InitialBalance = balance
            }).IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("123456789", true)]
        [InlineData("abc", false)]
        public void CancelCommandValidator_ExigeNumeroValido(string accountNumber, bool expected)
        {
            var validator = new CancelSavingsAccountCommandValidator();
            validator.Validate(new CancelSavingsAccountCommand { AccountNumber = accountNumber })
                     .IsValid.Should().Be(expected);
        }

        [Fact]
        public async Task GetSavingsAccountsQueryHandler_DevuelveSoloActivasPorDefecto()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, secondary, _) = await TestData.SeedAsync(context);

            secondary.Status = ArtemisBank.Core.Domain.Common.Enums.AccountStatus.Cancelada;
            await context.SaveChangesAsync();

            var handler = new GetSavingsAccountsQueryHandler(builder.SavingsAccountService());
            var result = await handler.Handle(new GetSavingsAccountsQuery(), CancellationToken.None);

            result.TotalRecords.Should().Be(2);
            result.Data.Should().OnlyContain(a => a.Status == "Activa");
        }

        [Fact]
        public async Task CreateSecondaryCommandHandler_CreaCuentaSecundaria()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            await TestData.SeedAsync(context);

            var handler = new CreateSecondarySavingsAccountCommandHandler(builder.SavingsAccountService());

            var created = await handler.Handle(new CreateSecondarySavingsAccountCommand
            {
                ClientId = TestData.ClientOne,
                InitialBalance = 750m,
                CreatedByUserId = TestData.AdminId
            }, CancellationToken.None);

            created.Type.Should().Be("Secundaria");
            created.Balance.Should().Be(750m);
        }

        [Fact]
        public async Task CancelCommandHandler_CancelaYTrasladaBalance()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, secondary, _) = await TestData.SeedAsync(context);

            var handler = new CancelSavingsAccountCommandHandler(builder.SavingsAccountService());

            await handler.Handle(new CancelSavingsAccountCommand
            {
                AccountNumber = secondary.AccountNumber,
                CancelledByUserId = TestData.AdminId
            }, CancellationToken.None);

            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(12500m);
        }

        [Fact]
        public async Task GetAccountTransactionsQueryHandler_CuentaInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);

            var handler = new GetAccountTransactionsQueryHandler(builder.SavingsAccountService());

            var act = async () => await handler.Handle(new GetAccountTransactionsQuery
            {
                AccountNumber = "999999999"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
