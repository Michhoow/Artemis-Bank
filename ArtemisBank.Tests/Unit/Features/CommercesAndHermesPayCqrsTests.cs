using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Features.Commerces.Commands;
using ArtemisBank.Core.Application.Features.Commerces.Queries;
using ArtemisBank.Core.Application.Features.HermesPay.Commands;
using ArtemisBank.Core.Application.Features.HermesPay.Queries;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class CommercesAndHermesPayCqrsTests
    {
        private static CreateCommerceCommand NuevoComercio(string rnc = "13012345678") => new()
        {
            Name = "Tienda de prueba",
            Rnc = rnc,
            Email = $"tienda{rnc}@correo.do",
            PhoneNumber = "8095550100",
            Description = "Comercio creado en pruebas",
            AdminUserId = TestData.AdminId
        };

        [Fact]
        public async Task CreateCommerceCommandHandler_CreaComercioActivo()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateCommerceCommandHandler(builder.CommerceService());

            var creado = await handler.Handle(NuevoComercio(), CancellationToken.None);

            creado.Id.Should().BeGreaterThan(0);
            creado.Name.Should().Be("Tienda de prueba");
            creado.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCommerceCommandHandler_RechazaRncDuplicado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateCommerceCommandHandler(builder.CommerceService());
            await handler.Handle(NuevoComercio("13011111111"), CancellationToken.None);

            var repetido = NuevoComercio("13011111111");
            repetido.Email = "otro@correo.do";

            var accion = async () => await handler.Handle(repetido, CancellationToken.None);

            await accion.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task CreateCommerceCommandHandler_RechazaCorreoDuplicado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var handler = new CreateCommerceCommandHandler(builder.CommerceService());
            var primero = NuevoComercio("13022222222");
            await handler.Handle(primero, CancellationToken.None);

            var repetido = NuevoComercio("13033333333");
            repetido.Email = primero.Email;

            var accion = async () => await handler.Handle(repetido, CancellationToken.None);

            await accion.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task GetCommercesQueryHandler_DevuelveElListadoPaginado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13044444444"), CancellationToken.None);

            var handler = new GetCommercesQueryHandler(builder.CommerceService());

            var resultado = await handler.Handle(
                new GetCommercesQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            resultado.Page.Should().Be(1);
            resultado.TotalRecords.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetCommerceByIdQueryHandler_DevuelveElDetalle()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13055555555"), CancellationToken.None);

            var handler = new GetCommerceByIdQueryHandler(builder.CommerceService());

            var detalle = await handler.Handle(new GetCommerceByIdQuery { Id = creado.Id },
                CancellationToken.None);

            detalle.Should().NotBeNull();
            detalle!.Id.Should().Be(creado.Id);
            detalle.Rnc.Should().Be("13055555555");
        }

        [Fact]
        public async Task UpdateCommerceCommandHandler_ActualizaDatosSinTocarElEstado()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13066666666"), CancellationToken.None);

            var handler = new UpdateCommerceCommandHandler(builder.CommerceService());

            var actualizado = await handler.Handle(new UpdateCommerceCommand
            {
                Id = creado.Id,
                Name = "Tienda renombrada",
                Rnc = "13066666666",
                Email = creado.Email,
                PhoneNumber = "8095559999",
                Description = "Datos actualizados"
            }, CancellationToken.None);

            actualizado.Name.Should().Be("Tienda renombrada");
            actualizado.PhoneNumber.Should().Be("8095559999");

            actualizado.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task SetCommerceStatusCommandHandler_DesactivaYReactivaElComercio()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var creado = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13077777777"), CancellationToken.None);

            var handler = new SetCommerceStatusCommandHandler(builder.CommerceService());

            var desactivado = await handler.Handle(new SetCommerceStatusCommand
            {
                Id = creado.Id, IsActive = false, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            desactivado.IsActive.Should().BeFalse();

            var reactivado = await handler.Handle(new SetCommerceStatusCommand
            {
                Id = creado.Id, IsActive = true, AdminUserId = TestData.AdminId
            }, CancellationToken.None);

            reactivado.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetCommerceTransactionsQueryHandler_DevuelveTransaccionesPaginadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var comercio = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13088888888"), CancellationToken.None);

            var handler = new GetCommerceTransactionsQueryHandler(builder.HermesPayService());

            var resultado = await handler.Handle(new GetCommerceTransactionsQuery
            {
                CommerceId = comercio.Id, Page = 1, PageSize = 20
            }, CancellationToken.None);

            resultado.Should().NotBeNull();
            resultado.Page.Should().Be(1);
        }

        [Fact]
        public async Task ProcessPaymentCommandHandler_RechazaTarjetaInexistente()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var comercio = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13099999999"), CancellationToken.None);

            var handler = new ProcessPaymentCommandHandler(builder.HermesPayService());

            var accion = async () => await handler.Handle(new ProcessPaymentCommand
            {
                CardNumber = "0000000000000000",
                MonthExpirationCard = "12",
                YearExpirationCard = "2030",
                Cvc = "123",
                TransactionAmount = 500m,
                CommerceId = comercio.Id
            }, CancellationToken.None);

            await accion.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ProcessPaymentCommandHandler_RechazaCvcIncorrecto()
        {
            using var context = TestContextFactory.CreateInMemory();
            await TestData.SeedAsync(context);
            var builder = new ModuleBuilder(context);

            var comercio = await new CreateCommerceCommandHandler(builder.CommerceService())
                .Handle(NuevoComercio("13010101010"), CancellationToken.None);

            var tarjeta = await builder.CardService().CreateAsync(
                new CreateCreditCardDto { ClientId = TestData.ClientOne, CreditLimit = 50000m },
                TestData.AdminId);

            var almacenada = await context.CreditCards.FirstAsync(c => c.Id == tarjeta.Card.Id);

            var handler = new ProcessPaymentCommandHandler(builder.HermesPayService());

            var accion = async () => await handler.Handle(new ProcessPaymentCommand
            {
                CardNumber = almacenada.CardNumber,
                MonthExpirationCard = almacenada.ExpirationMonth.ToString("D2"),
                YearExpirationCard = almacenada.ExpirationYear.ToString(),
                Cvc = "000",
                TransactionAmount = 500m,
                CommerceId = comercio.Id
            }, CancellationToken.None);

            await accion.Should().ThrowAsync<BusinessRuleException>();

            var trasIntento = await context.CreditCards.FirstAsync(c => c.Id == tarjeta.Card.Id);
            trasIntento.Debt.Should().Be(0m);
        }

        [Theory]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 21, false)]
        [InlineData(1, 20, true)]
        [InlineData(3, 10, true)]
        public void GetCommercesQueryValidator_ValidaPaginacion(int page, int pageSize, bool esperado)
        {
            var validator = new GetCommercesQueryValidator();

            var resultado = validator.Validate(new GetCommercesQuery { Page = page, PageSize = pageSize });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("Tienda", "13012345678", "t@correo.do", true)]
        [InlineData("", "13012345678", "t@correo.do", false)]
        [InlineData("Tienda", "", "t@correo.do", false)]
        [InlineData("Tienda", "13012345678", "sin-arroba", false)]
        public void CreateCommerceCommandValidator_ValidaCamposObligatorios(string nombre,
            string rnc, string correo, bool esperado)
        {
            var validator = new CreateCommerceCommandValidator();

            var resultado = validator.Validate(new CreateCommerceCommand
            {
                Name = nombre, Rnc = rnc, Email = correo, PhoneNumber = "8095550100"
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-5, false)]
        public void SetCommerceStatusCommandValidator_ExigeComercioValido(int id, bool esperado)
        {
            var validator = new SetCommerceStatusCommandValidator();

            var resultado = validator.Validate(new SetCommerceStatusCommand { Id = id, IsActive = true });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("1234567890123456", "12", "2030", "123", 500, true)]
        [InlineData("123", "12", "2030", "123", 500, false)]
        [InlineData("1234567890123456", "13", "2030", "123", 500, false)]
        [InlineData("1234567890123456", "12", "2030", "12", 500, false)]
        [InlineData("1234567890123456", "12", "2030", "123", 0, false)]
        [InlineData("1234567890123456", "12", "2030", "123", -50, false)]
        public void ProcessPaymentCommandValidator_ValidaDatosDeLaTarjeta(string numero,
            string mes, string anio, string cvc, decimal monto, bool esperado)
        {
            var validator = new ProcessPaymentCommandValidator();

            var resultado = validator.Validate(new ProcessPaymentCommand
            {
                CardNumber = numero,
                MonthExpirationCard = mes,
                YearExpirationCard = anio,
                Cvc = cvc,
                TransactionAmount = monto,
                CommerceId = 1
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, 1, 20, true)]
        [InlineData(0, 1, 20, false)]
        [InlineData(1, 0, 20, false)]
        [InlineData(1, 1, 21, false)]
        public void GetCommerceTransactionsQueryValidator_ValidaComercioYPaginacion(int comercioId,
            int page, int pageSize, bool esperado)
        {
            var validator = new GetCommerceTransactionsQueryValidator();

            var resultado = validator.Validate(new GetCommerceTransactionsQuery
            {
                CommerceId = comercioId, Page = page, PageSize = pageSize
            });

            resultado.IsValid.Should().Be(esperado);
        }
    }
}
