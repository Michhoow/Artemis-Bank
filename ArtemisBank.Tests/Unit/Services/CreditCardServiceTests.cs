using ArtemisBank.Core.Application.Common.Exceptions;
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
    /// <summary>Unit tests del servicio de tarjetas de credito y avance de efectivo (Manuel).</summary>
    public class CreditCardServiceTests
    {
        private static AssignCreditCardDto AssignRequest(decimal limit = 50000m)
            => new AssignCreditCardDto
            {
                ClientId = TestData.ClientOne,
                CreditLimit = limit,
                AssignedByUserId = TestData.AdminId
            };

        [Fact]
        public async Task Asignar_GeneraTarjetaDe16DigitosExpiracionA3AniosYDeudaCero()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            var created = await service.AssignAsync(AssignRequest());

            var card = await context.CreditCards.FirstAsync();
            card.CardNumber.Should().HaveLength(16);
            card.CardNumber.Should().MatchRegex("^[0-9]{16}$");
            card.Debt.Should().Be(0m);
            card.ExpirationDate.Should().MatchRegex(@"^\d{2}/\d{2}$");
            created.Status.Should().Be("Activa");
            created.MaskedCardNumber.Should().EndWith(card.LastFourDigits);
        }

        [Fact]
        public async Task Asignar_GuardaElCvcSoloComoHashNuncaEnTextoPlano()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            await service.AssignAsync(AssignRequest());

            var card = await context.CreditCards.FirstAsync();
            card.CvcHash.Should().NotBeNullOrEmpty();
            // El hash SHA-256 en hex ocupa 64 caracteres y no es un CVC de 3 digitos.
            card.CvcHash.Should().HaveLength(64);
            card.CvcHash.Should().NotMatchRegex("^[0-9]{3}$");
        }

        [Fact]
        public async Task EditarLimite_PorDebajoDeLaDeuda_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            var created = await service.AssignAsync(AssignRequest(limit: 50000m));
            var card = await context.CreditCards.FirstAsync();
            card.Debt = 30000m;
            await context.SaveChangesAsync();

            var act = async () => await service.UpdateLimitAsync(card.Id, 20000m);
            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task Cancelar_ConDeuda_Rechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            await service.AssignAsync(AssignRequest());
            var card = await context.CreditCards.FirstAsync();
            card.Debt = 100m;
            await context.SaveChangesAsync();

            var act = async () => await service.CancelAsync(card.Id);
            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task Cancelar_SinDeuda_MarcaCanceladaYConservaHistorial()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            await service.AssignAsync(AssignRequest());
            var card = await context.CreditCards.FirstAsync();

            await service.CancelAsync(card.Id);

            var reloaded = await context.CreditCards.FirstAsync();
            reloaded.Status.Should().Be(CreditCardStatus.Cancelada);
        }

        [Fact]
        public async Task Avance_AplicaInteresDe6_25YCargaMontoMasInteresALaTarjeta()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            await service.AssignAsync(AssignRequest(limit: 50000m));
            var card = await context.CreditCards.FirstAsync();

            var targetAccount = TestData.Account("900000009", TestData.ClientOne, 0m, AccountType.Principal);
            builder.GivenAccountByNumber(targetAccount);

            var result = await service.CashAdvanceAsync(new CashAdvanceDto
            {
                CardNumber = card.CardNumber,
                TargetAccountNumber = targetAccount.AccountNumber,
                Amount = 10000m,
                ClientId = TestData.ClientOne
            });

            result.Succeeded.Should().BeTrue();

            var reloaded = await context.CreditCards.FirstAsync();
            // 10000 + 6.25% = 10625 cargado a la tarjeta.
            reloaded.Debt.Should().Be(10625m);

            // Solo se acredita el monto (10000), no el interes, a la cuenta.
            builder.Transactions.Verify(t => t.RegisterExternalCreditAsync(
                It.Is<ExternalCreditRequest>(r =>
                    r.Amount == 10000m && r.Operation == TransactionOperation.AvanceEfectivo),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Avance_QueExcedeElCreditoDisponible_RechazaYRegistraConsumoRechazado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new CreditProductsServiceBuilder(context);
            var service = builder.CreditCardService();

            await service.AssignAsync(AssignRequest(limit: 5000m));
            var card = await context.CreditCards.FirstAsync();

            var targetAccount = TestData.Account("900000009", TestData.ClientOne, 0m, AccountType.Principal);
            builder.GivenAccountByNumber(targetAccount);

            var result = await service.CashAdvanceAsync(new CashAdvanceDto
            {
                CardNumber = card.CardNumber,
                TargetAccountNumber = targetAccount.AccountNumber,
                Amount = 5000m, // 5000 + interes > 5000 de limite
                ClientId = TestData.ClientOne
            });

            result.Succeeded.Should().BeFalse();

            var reloaded = await context.CreditCards.FirstAsync();
            reloaded.Debt.Should().Be(0m); // el rechazo no afecta la deuda

            var consumos = await context.Consumptions.ToListAsync();
            consumos.Should().ContainSingle(k => k.Status == ConsumptionStatus.Rechazado);

            // Nunca se acredito nada a la cuenta.
            builder.Transactions.Verify(t => t.RegisterExternalCreditAsync(
                It.IsAny<ExternalCreditRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
