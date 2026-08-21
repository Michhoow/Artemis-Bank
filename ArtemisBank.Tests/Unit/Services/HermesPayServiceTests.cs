using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Dtos.HermesPay;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    public class HermesPayServiceTests
    {
        private static async Task<(int CommerceId, string CardNumber, string Cvc, string CommerceAccount,
            string Month, string Year)>
            SetupAsync(ModuleBuilder builder, decimal cardLimit = 50000m, bool commerceActive = true)
        {
            var created = await builder.CardService().CreateAsync(new CreateCreditCardDto
            {
                ClientId = TestData.ClientOne,
                CreditLimit = cardLimit
            }, TestData.AdminId);

            var card = await builder.Context.CreditCards.FirstAsync();

            var commerceAccount = TestData.Account("300000001", "usuario-comercio", 0m,
                AccountType.Principal);
            builder.Context.SavingsAccounts.Add(commerceAccount);

            var commerce = new Commerce
            {
                Name = "Supermercado Hermes",
                Rnc = "130123456",
                Email = "comercio@artemisbank.do",
                Phone = "8095551234",
                UserId = "usuario-comercio",
                AccountNumber = commerceAccount.AccountNumber,
                IsActive = commerceActive,
                CreatedAt = DateTime.Now
            };
            builder.Context.Commerces.Add(commerce);
            await builder.Context.SaveChangesAsync();

            return (commerce.Id, card.CardNumber, created.Cvc, commerceAccount.AccountNumber,
                card.ExpirationMonth.ToString("00"), card.ExpirationYear.ToString());
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConDatosValidos_Aprueba()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var result = await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            result.Approved.Should().BeTrue();
            result.Amount.Should().Be(1500m);
            result.Reference.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task ProcessPaymentAsync_AprobadoAumentaLaDeudaDeLaTarjeta()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            (await context.CreditCards.FirstAsync()).Debt.Should().Be(1500m);
        }

        [Fact]
        public async Task ProcessPaymentAsync_AprobadoAcreditaAlComercio()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, commerceAccount, month, year) = await SetupAsync(builder);

            await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            var account = await context.SavingsAccounts
                .FirstAsync(a => a.AccountNumber == commerceAccount);

            account.Balance.Should().Be(1500m);

            var movement = await context.Transactions
                .FirstOrDefaultAsync(t => t.SavingsAccountId == account.Id);

            movement.Should().NotBeNull();
            movement!.Type.Should().Be(TransactionType.Credito);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ElOrigenRegistraSoloLosUltimosCuatroDigitos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, commerceAccount, month, year) = await SetupAsync(builder);

            await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            var account = await context.SavingsAccounts.FirstAsync(a => a.AccountNumber == commerceAccount);
            var movement = await context.Transactions.FirstAsync(t => t.SavingsAccountId == account.Id);

            movement.Origin.Should().NotBe(cardNumber);
            movement.Origin.Should().Be(cardNumber[^4..]);
        }

        [Fact]
        public async Task ProcessPaymentAsync_AprobadoRegistraElConsumo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            var consumption = await context.CardConsumptions.SingleAsync();
            consumption.Status.Should().Be(ConsumptionStatus.Aprobado);
            consumption.Type.Should().Be(ConsumptionType.Consumo);
            consumption.CommerceName.Should().Be("Supermercado Hermes");
        }

        [Fact]
        public async Task ProcessPaymentAsync_SinCreditoSuficiente_RechazaSinMoverDinero()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, commerceAccount, month, year) = await SetupAsync(builder, cardLimit: 1000m);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 5000m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*crédito*");

            (await context.CreditCards.FirstAsync()).Debt.Should().Be(0m);
            (await context.SavingsAccounts.FirstAsync(a => a.AccountNumber == commerceAccount))
                .Balance.Should().Be(0m);
        }

        [Fact]
        public async Task ProcessPaymentAsync_RechazadoQuedaEnElHistorialConSuMotivo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder, cardLimit: 1000m);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 5000m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();

            var consumption = await context.CardConsumptions.SingleAsync();
            consumption.Status.Should().Be(ConsumptionStatus.Rechazado);
            consumption.RejectionReason.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConCvcIncorrecto_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var wrongCvc = cvc == "999" ? "111" : "999";

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = wrongCvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
            (await context.CreditCards.FirstAsync()).Debt.Should().Be(0m);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConTarjetaInexistente_DaElMismoMensajeQueUnCvcMalo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var missing = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = "9999999999999999",
                Cvc = "123",
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            var wrongCvc = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc == "999" ? "111" : "999",
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            var first = (await missing.Should().ThrowAsync<BusinessRuleException>()).Which.Message;
            var second = (await wrongCvc.Should().ThrowAsync<BusinessRuleException>()).Which.Message;

            first.Should().Be(second);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConExpiracionQueNoCorrespondeALaTarjeta_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month == "01" ? "02" : "01",
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
            (await context.CreditCards.FirstAsync()).Debt.Should().Be(0m);
        }

        [Theory]
        [InlineData("00")]
        [InlineData("13")]
        [InlineData("ab")]
        public async Task ProcessPaymentAsync_ConMesDeExpiracionInvalido_Rechaza(string badMonth)
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, _, year) = await SetupAsync(builder);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = badMonth,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task GetTransactionsAsync_DevuelveLosCobrosDelComercioSinNumerosCompletos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 1500m
            }, commerceId, "usuario-comercio");

            var page = await builder.HermesPayService().GetTransactionsAsync(commerceId, 1, 20);

            page.CommerceId.Should().Be(commerceId);
            page.CommerceName.Should().Be("Supermercado Hermes");
            page.TotalRecords.Should().Be(1);
            page.Data.Should().ContainSingle()
                .Which.CardLastFourDigits.Should().Be(cardNumber[^4..]);
            page.Data.Should().OnlyContain(t => t.CardLastFourDigits.Length == 4);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConComercioInactivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder, commerceActive: false);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConComercioInexistente_LanzaNotFound()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (_, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, 9999, "usuario-comercio");

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Theory]
        [InlineData("123")]
        [InlineData("12345678901234567")]
        [InlineData("abcdabcdabcdabcd")]
        public async Task ProcessPaymentAsync_ConNumeroDeTarjetaMalFormado_Rechaza(string cardNumber)
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, _, _, _, month, year) = await SetupAsync(builder);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = "123",
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConMontoNoPositivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 0m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_ConTarjetaCancelada_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var card = await context.CreditCards.FirstAsync();
            card.Status = CardStatus.Cancelada;
            await context.SaveChangesAsync();

            var act = () => builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_LaRespuestaNoExponeElNumeroCompletoNiElCvc()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);
            var (commerceId, cardNumber, cvc, _, month, year) = await SetupAsync(builder);

            var result = await builder.HermesPayService().ProcessPaymentAsync(new HermesPayRequestDto
            {
                CardNumber = cardNumber,
                Cvc = cvc,
                MonthExpirationCard = month,
                YearExpirationCard = year,
                TransactionAmount = 100m
            }, commerceId, "usuario-comercio");

            result.CardLastFourDigits.Should().Be(cardNumber[^4..]);
            result.CardLastFourDigits.Should().HaveLength(4);
        }
    }
}
