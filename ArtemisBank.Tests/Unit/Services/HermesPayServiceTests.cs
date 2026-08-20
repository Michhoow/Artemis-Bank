using ArtemisBank.Core.Application.Dtos.CreditCards;
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
    /// <summary>Unit tests del procesador Hermes Pay (Manuel).</summary>
    public class HermesPayServiceTests
    {
        private const string Cvc = "123";

        private static async Task<CreditCard> SeedCardAsync(CreditProductsServiceBuilder builder,
            decimal limit = 50000m, decimal debt = 0m)
        {
            var card = new CreditCard
            {
                CardNumber = "4000000000000001",
                ClientId = TestData.ClientOne,
                CreditLimit = limit,
                Debt = debt,
                CvcHash = builder.Crypto.Hash(Cvc),
                ExpirationDate = DateTime.Now.AddYears(3).ToString("MM/yy"),
                Status = CreditCardStatus.Activa,
                CreatedAt = DateTime.Now
            };
            builder.Context.CreditCards.Add(card);
            await builder.Context.SaveChangesAsync();
            return card;
        }

        private static Commerce Commerce(bool active = true, string userId = "comercio-user-1")
            => new Commerce
            {
                Id = 5,
                Name = "Comercio Demo",
                Email = "comercio@artemisbank.do",
                UserId = userId,
                AccountNumber = "800000001",
                IsActive = active,
                CreatedAt = DateTime.Now
            };

        private static ProcessPaymentDto Payment(int commerceId, string cardNumber, decimal amount,
            string cvc = Cvc, bool isCommerce = false)
        {
            var exp = DateTime.Now.AddYears(3);
            return new ProcessPaymentDto
            {
                CommerceId = commerceId,
                CardNumber = cardNumber,
                MonthExpirationCard = exp.ToString("MM"),
                YearExpirationCard = exp.ToString("yy"),
                Cvc = cvc,
                TransactionAmount = amount,
                AuthenticatedUserId = TestData.AdminId,
                IsCommerceRole = isCommerce
            };
        }

        [Fact]
        public async Task Pago_Valido_AumentaDeudaRegistraConsumoYAcreditaAlComercio()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var commerce = Commerce();
            builder.Commerces.Setup(c => c.GetByIdAsync(commerce.Id)).ReturnsAsync(commerce);
            builder.Accounts.Setup(a => a.GetActiveByClientAsync(commerce.UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Core.Application.Dtos.SavingsAccounts.SavingsAccountDto>
                   {
                       new() { AccountNumber = commerce.AccountNumber, Type = "Principal", Status = "Activa" }
                   });

            var card = await SeedCardAsync(builder);
            var service = builder.HermesPayService();

            var result = await service.ProcessPaymentAsync(Payment(commerce.Id, card.CardNumber, 8000m));

            result.Succeeded.Should().BeTrue();
            var reloaded = await context.CreditCards.FirstAsync();
            reloaded.Debt.Should().Be(8000m);

            var consumos = await context.Consumptions.ToListAsync();
            consumos.Should().ContainSingle(k => k.Status == ConsumptionStatus.Aprobado && k.CommerceId == commerce.Id);

            builder.Transactions.Verify(t => t.RegisterExternalCreditAsync(
                It.Is<ExternalCreditRequest>(r =>
                    r.Amount == 8000m && r.Operation == TransactionOperation.PagoComercio),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Pago_CvcIncorrecto_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var commerce = Commerce();
            builder.Commerces.Setup(c => c.GetByIdAsync(commerce.Id)).ReturnsAsync(commerce);
            builder.Accounts.Setup(a => a.GetActiveByClientAsync(commerce.UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Core.Application.Dtos.SavingsAccounts.SavingsAccountDto>
                   {
                       new() { AccountNumber = commerce.AccountNumber, Type = "Principal", Status = "Activa" }
                   });

            var card = await SeedCardAsync(builder);
            var service = builder.HermesPayService();

            var result = await service.ProcessPaymentAsync(Payment(commerce.Id, card.CardNumber, 8000m, cvc: "999"));

            result.Succeeded.Should().BeFalse();
            (await context.CreditCards.FirstAsync()).Debt.Should().Be(0m);
        }

        [Fact]
        public async Task Pago_ComercioInactivo_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var commerce = Commerce(active: false);
            builder.Commerces.Setup(c => c.GetByIdAsync(commerce.Id)).ReturnsAsync(commerce);

            var card = await SeedCardAsync(builder);
            var service = builder.HermesPayService();

            var result = await service.ProcessPaymentAsync(Payment(commerce.Id, card.CardNumber, 8000m));

            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task Pago_QueExcedeElCredito_RechazaYRegistraRechazadoSinAfectarBalances()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var commerce = Commerce();
            builder.Commerces.Setup(c => c.GetByIdAsync(commerce.Id)).ReturnsAsync(commerce);
            builder.Accounts.Setup(a => a.GetActiveByClientAsync(commerce.UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Core.Application.Dtos.SavingsAccounts.SavingsAccountDto>
                   {
                       new() { AccountNumber = commerce.AccountNumber, Type = "Principal", Status = "Activa" }
                   });

            var card = await SeedCardAsync(builder, limit: 5000m, debt: 4000m); // disponible 1000
            var service = builder.HermesPayService();

            var result = await service.ProcessPaymentAsync(Payment(commerce.Id, card.CardNumber, 3000m));

            result.Succeeded.Should().BeFalse();
            (await context.CreditCards.FirstAsync()).Debt.Should().Be(4000m); // intacta

            var consumos = await context.Consumptions.ToListAsync();
            consumos.Should().ContainSingle(k => k.Status == ConsumptionStatus.Rechazado);

            builder.Transactions.Verify(t => t.RegisterExternalCreditAsync(
                It.IsAny<ExternalCreditRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
