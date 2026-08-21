using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class CreditCardServiceTests
    {
        private static CreateCreditCardDto Request(string clientId, decimal limit = 50000m)
            => new CreateCreditCardDto { ClientId = clientId, CreditLimit = limit };

        [Fact]
        public async Task CreateAsync_GeneraUnNumeroDeDieciseisDigitos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await builder.CardService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.CardNumber.Should().MatchRegex(@"^\d{16}$");
        }

        [Fact]
        public async Task CreateAsync_LaExpiracionEsATresAnios()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await builder.CardService().CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.ExpirationYear.Should().Be(DateTime.Now.Year + 3);
            stored.ExpirationMonth.Should().Be(DateTime.Now.Month);
        }

        [Fact]
        public async Task CreateAsync_DevuelveElCvcUnaSolaVezYDeTresDigitos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            created.Cvc.Should().MatchRegex(@"^\d{3}$");
        }

        [Fact]
        public async Task CreateAsync_NuncaGuardaElCvcEnTextoPlano()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();

            stored.CvcHash.Should().NotBe(created.Cvc);
            stored.CvcHash.Should().NotContain(created.Cvc);
            stored.CvcSalt.Should().NotContain(created.Cvc);
            stored.CvcHash.Should().NotBeNullOrWhiteSpace();
            stored.CvcSalt.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CreateAsync_ElHashDelCvcSeVerificaContraElCodigoOriginal()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);
            var stored = await context.CreditCards.FirstAsync();

            builder.Crypto.Verify(created.Cvc, stored.CvcSalt, stored.CvcHash).Should().BeTrue();
            builder.Crypto.Verify("000", stored.CvcSalt, stored.CvcHash)
                   .Should().Be(created.Cvc == "000");
        }

        [Fact]
        public async Task CreateAsync_DosTarjetasNoCompartenSal()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.CardService();

            await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);
            await service.CreateAsync(Request(TestData.ClientTwo), TestData.AdminId);

            var salts = await context.CreditCards.Select(c => c.CvcSalt).ToListAsync();
            salts.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task CreateAsync_LaTarjetaNaceActivaYSinDeuda()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            created.Card.Status.Should().Be("Activa");
            created.Card.Debt.Should().Be(0m);
            created.Card.AvailableCredit.Should().Be(created.Card.CreditLimit);
        }

        [Fact]
        public async Task CreateAsync_ElDtoNoExponeElNumeroCompleto()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);
            var stored = await context.CreditCards.FirstAsync();

            created.Card.MaskedNumber.Should().NotBe(stored.CardNumber);
            created.Card.MaskedNumber.Should().EndWith(stored.LastFourDigits);
            created.Card.LastFourDigits.Should().HaveLength(4);
        }

        [Fact]
        public async Task CreateAsync_ConClienteInactivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            builder.Users.Setup(u => u.GetByIdAsync(TestData.ClientOne))
                   .ReturnsAsync(TestData.User(TestData.ClientOne, "001", isActive: false));

            var act = () => builder.CardService()
                .CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-500)]
        public async Task CreateAsync_ConLimiteNoPositivo_Rechaza(decimal limit)
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var act = () => builder.CardService()
                .CreateAsync(Request(TestData.ClientOne, limit), TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*mayor que cero*");
        }

        [Fact]
        public async Task UpdateLimitAsync_ConNuevoLimiteMenorQueLaDeuda_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.CardService();

            var created = await service.CreateAsync(Request(TestData.ClientOne, 50000m), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.Debt = 20000m;
            await context.SaveChangesAsync();

            var act = () => service.UpdateLimitAsync(created.Card.Id, 15000m, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*menor que la deuda*");
        }

        [Fact]
        public async Task UpdateLimitAsync_ConLimiteIgualALaDeuda_EsValido()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.CardService();

            var created = await service.CreateAsync(Request(TestData.ClientOne, 50000m), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.Debt = 20000m;
            await context.SaveChangesAsync();

            var updated = await service.UpdateLimitAsync(created.Card.Id, 20000m, TestData.AdminId);

            updated.CreditLimit.Should().Be(20000m);
            updated.AvailableCredit.Should().Be(0m);
        }

        [Fact]
        public async Task CancelAsync_ConDeudaPendiente_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.CardService();

            var created = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.Debt = 0.01m;
            await context.SaveChangesAsync();

            var act = () => service.CancelAsync(created.Card.Id, TestData.AdminId);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*deuda*");
        }

        [Fact]
        public async Task CancelAsync_SinDeuda_CancelaSinBorrarElRegistro()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var service = builder.CardService();

            var created = await service.CreateAsync(Request(TestData.ClientOne), TestData.AdminId);

            await service.CancelAsync(created.Card.Id, TestData.AdminId);

            var stored = await context.CreditCards.FirstAsync();
            stored.Status.Should().Be(CardStatus.Cancelada);

            (await context.CreditCards.CountAsync()).Should().Be(1);
        }

        private async Task<(int CardId, string AccountNumber)> SetupAdvanceAsync(ModuleBuilder builder,
            decimal limit = 50000m)
        {
            var created = await builder.CardService()
                .CreateAsync(Request(TestData.ClientOne, limit), TestData.AdminId);

            var account = await builder.Context.SavingsAccounts
                .FirstAsync(a => a.ClientId == TestData.ClientOne && a.Type == AccountType.Principal);

            return (created.Card.Id, account.AccountNumber);
        }

        [Fact]
        public async Task CashAdvanceAsync_CargaElMontoMasSeisComaVeinticincoPorCiento()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            var result = await builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 10000m
            }, TestData.ClientOne);

            result.InterestAmount.Should().Be(625.00m);
            result.TotalCharged.Should().Be(10625.00m);
            result.Amount.Should().Be(10000m);
        }

        [Fact]
        public async Task CashAdvanceAsync_AcreditaSoloElMontoSolicitadoEnLaCuenta()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var balanceBefore = principal.Balance;
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            await builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 10000m
            }, TestData.ClientOne);

            var updated = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);

            updated.Balance.Should().Be(balanceBefore + 10000m);
        }

        [Fact]
        public async Task CashAdvanceAsync_AumentaLaDeudaConElTotalCargado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            await builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 10000m
            }, TestData.ClientOne);

            var stored = await context.CreditCards.FirstAsync();
            stored.Debt.Should().Be(10625.00m);
        }

        [Fact]
        public async Task CashAdvanceAsync_RegistraDosConsumosAvanceEInteres()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            await builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 10000m
            }, TestData.ClientOne);

            var consumptions = await context.CardConsumptions.ToListAsync();

            consumptions.Should().Contain(c => c.Type == ConsumptionType.AvanceEfectivo && c.Amount == 10000m);
            consumptions.Should().Contain(c => c.Type == ConsumptionType.InteresAvance && c.Amount == 625.00m);
            consumptions.Should().OnlyContain(c => c.Status == ConsumptionStatus.Aprobado);
        }

        [Fact]
        public async Task CashAdvanceAsync_ConCreditoInsuficiente_RechazaYNoMueveNada()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var balanceBefore = principal.Balance;
            var builder = new ModuleBuilder(context);

            var (cardId, accountNumber) = await SetupAdvanceAsync(builder, limit: 10000m);

            var act = () => builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 10000m
            }, TestData.ClientOne);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*crédito disponible*");

            var updatedAccount = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);
            updatedAccount.Balance.Should().Be(balanceBefore);
            (await context.CreditCards.FirstAsync()).Debt.Should().Be(0m);
        }

        [Fact]
        public async Task CashAdvanceAsync_HaciaCuentaDeOtroCliente_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (_, _, other) = await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, _) = await SetupAdvanceAsync(builder);

            var act = () => builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,

                TargetAccountNumber = other.AccountNumber,
                Amount = 1000m
            }, TestData.ClientOne);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CashAdvanceAsync_ConTarjetaDeOtroCliente_LanzaForbidden()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            var act = () => builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 1000m
            }, TestData.ClientTwo);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task CashAdvanceAsync_ConMontoNoPositivo_Rechaza(decimal amount)
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            var act = () => builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = amount
            }, TestData.ClientOne);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CashAdvanceAsync_ConTarjetaCancelada_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (cardId, accountNumber) = await SetupAdvanceAsync(builder);

            var stored = await context.CreditCards.FirstAsync();
            stored.Status = CardStatus.Cancelada;
            await context.SaveChangesAsync();

            var act = () => builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
            {
                CreditCardId = cardId,
                TargetAccountNumber = accountNumber,
                Amount = 1000m
            }, TestData.ClientOne);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }
    }
}
