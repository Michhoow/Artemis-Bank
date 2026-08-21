using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Features.CreditCards.Commands;
using ArtemisBank.Core.Application.Features.CreditCards.Queries;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class CreditCardsCqrsTests
    {
        [Fact]
        public async Task CreateCreditCardCommandHandler_EmiteTarjetaYDevuelveElCvcUnaSolaVez()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateCreditCardCommandHandler(builder.CardService());

            var creada = await handler.Handle(new CreateCreditCardCommand
            {
                ClientId = TestData.ClientOne,
                CreditLimit = 50000m,
                AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            creada.Cvc.Should().MatchRegex(@"^\d{3}$");
            creada.Card.CreditLimit.Should().Be(50000m);
            creada.Card.Debt.Should().Be(0m);
            creada.Card.AvailableCredit.Should().Be(50000m);

            var almacenada = await context.CreditCards.FirstAsync();
            almacenada.CvcHash.Should().NotBeNullOrWhiteSpace();
            almacenada.CvcHash.Should().NotBe(creada.Cvc);
        }

        [Fact]
        public async Task CreateCreditCardCommandHandler_NoExponeElNumeroCompleto()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateCreditCardCommandHandler(builder.CardService());

            var creada = await handler.Handle(new CreateCreditCardCommand
            {
                ClientId = TestData.ClientOne, CreditLimit = 50000m, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            creada.Card.MaskedNumber.Should().NotMatchRegex(@"\d{16}");
            creada.Card.LastFourDigits.Should().MatchRegex(@"^\d{4}$");
        }

        [Fact]
        public async Task GetCreditCardsQueryHandler_DevuelveElListadoPaginado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 30000m },
                TestData.AdminId);

            var handler = new GetCreditCardsQueryHandler(builder.CardService());

            var resultado = await handler.Handle(
                new GetCreditCardsQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            resultado.TotalRecords.Should().BeGreaterThan(0);
            resultado.Page.Should().Be(1);
            resultado.Data.Should().OnlyContain(c => c.MaskedNumber.Contains("*"));
        }

        [Fact]
        public async Task GetCreditCardsQueryHandler_FiltraPorCedulaDelCliente()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 30000m },
                TestData.AdminId);

            var handler = new GetCreditCardsQueryHandler(builder.CardService());

            var resultado = await handler.Handle(new GetCreditCardsQuery
            {
                Page = 1, PageSize = 20, Identification = "001" + TestData.ClientOne
            }, CancellationToken.None);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCreditCardByIdQueryHandler_DevuelveDetalleConConsumos()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 40000m },
                TestData.AdminId);

            var handler = new GetCreditCardByIdQueryHandler(builder.CardService());

            var detalle = await handler.Handle(
                new GetCreditCardByIdQuery { Id = emitida.Card.Id }, CancellationToken.None);

            detalle.Should().NotBeNull();
            detalle!.Id.Should().Be(emitida.Card.Id);
            detalle.Consumptions.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateCardLimitCommandHandler_ActualizaElLimiteYRecalculaDisponible()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 20000m },
                TestData.AdminId);

            var handler = new UpdateCardLimitCommandHandler(builder.CardService());

            var actualizada = await handler.Handle(new UpdateCardLimitCommand
            {
                CreditCardId = emitida.Card.Id, CreditLimit = 35000m, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            actualizada.CreditLimit.Should().Be(35000m);
            actualizada.AvailableCredit.Should().Be(35000m - actualizada.Debt);
        }

        [Fact]
        public async Task UpdateCardLimitCommandHandler_RechazaLimiteMenorQueLaDeuda()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 50000m },
                TestData.AdminId);

            var tarjeta = await context.CreditCards.FirstAsync(c => c.Id == emitida.Card.Id);
            tarjeta.Debt = 10000m;
            await context.SaveChangesAsync();

            var handler = new UpdateCardLimitCommandHandler(builder.CardService());

            var accion = async () => await handler.Handle(new UpdateCardLimitCommand
            {
                CreditCardId = emitida.Card.Id, CreditLimit = 5000m, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            await accion.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CancelCreditCardCommandHandler_CancelaTarjetaSinDeuda()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 25000m },
                TestData.AdminId);

            var handler = new CancelCreditCardCommandHandler(builder.CardService());

            await handler.Handle(new CancelCreditCardCommand
            {
                CreditCardId = emitida.Card.Id, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            var cancelada = await context.CreditCards.FirstAsync(c => c.Id == emitida.Card.Id);
            cancelada.Status.Should().Be(CardStatus.Cancelada);
        }

        [Fact]
        public async Task CancelCreditCardCommandHandler_RechazaCancelarConDeudaPendiente()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 25000m },
                TestData.AdminId);

            var tarjeta = await context.CreditCards.FirstAsync(c => c.Id == emitida.Card.Id);
            tarjeta.Debt = 500m;
            await context.SaveChangesAsync();

            var handler = new CancelCreditCardCommandHandler(builder.CardService());

            var accion = async () => await handler.Handle(new CancelCreditCardCommand
            {
                CreditCardId = emitida.Card.Id, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            await accion.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CashAdvanceCommandHandler_CobraElInteresDelSeisVeinticinco()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, _, _) = await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var emitida = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 50000m },
                TestData.AdminId);

            var handler = new CashAdvanceCommandHandler(builder.CardService());

            var resultado = await handler.Handle(new CashAdvanceCommand
            {
                CreditCardId = emitida.Card.Id,
                TargetAccountNumber = principal.AccountNumber,
                Amount = 1000m,
                ClientId = TestData.ClientOne
            }, CancellationToken.None);

            resultado.Amount.Should().Be(1000m);
            resultado.InterestAmount.Should().Be(62.50m);
            resultado.TotalCharged.Should().Be(1062.50m);
        }

        [Theory]
        [InlineData(0, 20, "activa", false)]
        [InlineData(1, 0, "activa", false)]
        [InlineData(1, 21, "activa", false)]
        [InlineData(1, 20, "inventado", false)]
        [InlineData(1, 20, "activa", true)]
        [InlineData(2, 20, "cancelada", true)]
        [InlineData(1, 20, "todas", true)]
        public void GetCreditCardsQueryValidator_ValidaParametros(int page, int pageSize,
            string status, bool esperado)
        {
            var validator = new GetCreditCardsQueryValidator();

            var resultado = validator.Validate(new GetCreditCardsQuery
            {
                Page = page, PageSize = pageSize, Status = status
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("cliente-1", 50000, true)]
        [InlineData("", 50000, false)]
        [InlineData("cliente-1", 0, false)]
        [InlineData("cliente-1", -100, false)]
        public void CreateCreditCardCommandValidator_ValidaClienteYLimite(string clienteId,
            decimal limite, bool esperado)
        {
            var validator = new CreateCreditCardCommandValidator();

            var resultado = validator.Validate(new CreateCreditCardCommand
            {
                ClientId = clienteId, CreditLimit = limite
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, 30000, true)]
        [InlineData(0, 30000, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, -50, false)]
        public void UpdateCardLimitCommandValidator_ValidaTarjetaYLimite(int tarjetaId,
            decimal limite, bool esperado)
        {
            var validator = new UpdateCardLimitCommandValidator();

            var resultado = validator.Validate(new UpdateCardLimitCommand
            {
                CreditCardId = tarjetaId, CreditLimit = limite
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-2, false)]
        public void CancelCreditCardCommandValidator_ExigeTarjetaValida(int tarjetaId, bool esperado)
        {
            var validator = new CancelCreditCardCommandValidator();

            var resultado = validator.Validate(new CancelCreditCardCommand { CreditCardId = tarjetaId });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, "123456789", 500, true)]
        [InlineData(0, "123456789", 500, false)]
        [InlineData(1, "", 500, false)]
        [InlineData(1, "123456789", 0, false)]
        [InlineData(1, "123456789", -10, false)]
        public void CashAdvanceCommandValidator_ValidaTarjetaCuentaYMonto(int tarjetaId,
            string cuenta, decimal monto, bool esperado)
        {
            var validator = new CashAdvanceCommandValidator();

            var resultado = validator.Validate(new CashAdvanceCommand
            {
                CreditCardId = tarjetaId,
                TargetAccountNumber = cuenta,
                Amount = monto,
                ClientId = "cliente-1"
            });

            resultado.IsValid.Should().Be(esperado);
        }
    }
}
