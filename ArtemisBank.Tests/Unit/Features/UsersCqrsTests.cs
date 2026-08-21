using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Features.Users.Commands;
using ArtemisBank.Core.Application.Features.Users.Queries;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class UsersCqrsTests
    {
        [Fact]
        public async Task CreateUserCommandHandler_TrasladaTodosLosCamposAlServicio()
        {
            var servicio = new Mock<IUserManagementService>();
            CreateUserRequest? recibido = null;

            servicio.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<string?>(),
                                              It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Callback<CreateUserRequest, string?, bool, CancellationToken>(
                        (r, _, _, _) => recibido = r)
                    .ReturnsAsync(UserOperationResult.Success("nuevo-id"));

            var handler = new CreateUserCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new CreateUserCommand
            {
                FirstName = "Ana",
                LastName = "Perez",
                Identification = "00112345678",
                Email = "ana@correo.do",
                UserName = "ana@correo.do",
                Password = "Clave@12345!",
                ConfirmPassword = "Clave@12345!",
                Role = "Cliente",
                InitialAmount = 5000m,
                CreatedByUserId = "admin-1"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
            resultado.UserId.Should().Be("nuevo-id");

            recibido.Should().NotBeNull();
            recibido!.FirstName.Should().Be("Ana");
            recibido.Identification.Should().Be("00112345678");
            recibido.Role.Should().Be("Cliente");
            recibido.InitialAmount.Should().Be(5000m);
        }

        [Fact]
        public async Task CreateUserCommandHandler_PropagaElFalloDelServicio()
        {
            var servicio = new Mock<IUserManagementService>();
            servicio.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<string?>(),
                                              It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(UserOperationResult.Failure("La cédula ya está registrada."));

            var handler = new CreateUserCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new CreateUserCommand
            {
                FirstName = "Ana", LastName = "Perez", Identification = "00112345678",
                Email = "ana@correo.do", UserName = "ana@correo.do",
                Password = "Clave@12345!", ConfirmPassword = "Clave@12345!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Error.Should().Contain("cédula");
        }

        [Fact]
        public async Task CreateCommerceUserCommandHandler_EnviaElComercioIndicado()
        {
            var servicio = new Mock<IUserManagementService>();
            var comercioRecibido = 0;

            servicio.Setup(s => s.CreateCommerceUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<int>(),
                                                          It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Callback<CreateUserRequest, int, bool, CancellationToken>(
                        (_, id, _, _) => comercioRecibido = id)
                    .ReturnsAsync(UserOperationResult.Success("usuario-comercio"));

            var handler = new CreateCommerceUserCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new CreateCommerceUserCommand
            {
                CommerceId = 7,
                FirstName = "Tienda", LastName = "Central",
                Identification = "13012345678",
                Email = "tienda@correo.do", UserName = "tienda@correo.do",
                Password = "Clave@12345!", ConfirmPassword = "Clave@12345!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
            comercioRecibido.Should().Be(7);
        }

        [Fact]
        public async Task UpdateUserCommandHandler_NoEnviaRolPorqueEsInmutable()
        {
            var servicio = new Mock<IUserManagementService>();
            UpdateUserRequest? recibido = null;

            servicio.Setup(s => s.UpdateAsync(It.IsAny<UpdateUserRequest>(), It.IsAny<string?>(),
                                              It.IsAny<CancellationToken>()))
                    .Callback<UpdateUserRequest, string?, CancellationToken>((r, _, _) => recibido = r)
                    .ReturnsAsync(UserOperationResult.Success("usuario-1"));

            var handler = new UpdateUserCommandHandler(servicio.Object);

            await handler.Handle(new UpdateUserCommand
            {
                UserId = "usuario-1",
                FirstName = "Ana Maria", LastName = "Perez",
                Identification = "00112345678",
                Email = "ana@correo.do", UserName = "ana@correo.do"
            }, CancellationToken.None);

            recibido.Should().NotBeNull();
            recibido!.FirstName.Should().Be("Ana Maria");
            typeof(UpdateUserRequest).GetProperty("Role").Should().BeNull(
                "el rol no debe poder modificarse desde la actualizacion de usuario");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetUserStatusCommandHandler_TrasladaEstadoYLlamante(bool activo)
        {
            var servicio = new Mock<IUserManagementService>();
            string? objetivo = null; var estado = !activo; string? llamante = null;

            servicio.Setup(s => s.SetActiveAsync(It.IsAny<string>(), It.IsAny<bool>(),
                                                 It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .Callback<string, bool, string?, CancellationToken>(
                        (t, e, c, _) => { objetivo = t; estado = e; llamante = c; })
                    .ReturnsAsync(UserOperationResult.Success("usuario-1"));

            var handler = new SetUserStatusCommandHandler(servicio.Object);

            await handler.Handle(new SetUserStatusCommand
            {
                TargetUserId = "usuario-1", IsActive = activo, CallerUserId = "admin-1"
            }, CancellationToken.None);

            objetivo.Should().Be("usuario-1");
            estado.Should().Be(activo);

            llamante.Should().Be("admin-1");
        }

        [Fact]
        public async Task GetUsersQueryHandler_DevuelveElPaginadoDelServicio()
        {
            var servicio = new Mock<IUserManagementService>();

            servicio.Setup(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                                                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PagedResult<UserInfoDto>
                    {
                        Page = 2,
                        PageSize = 20,
                        TotalRecords = 25,
                        Data = new List<UserInfoDto>
                        {
                            new() { Id = "u1", UserName = "uno@correo.do", Role = "Cliente", IsActive = true }
                        }
                    });

            var handler = new GetUsersQueryHandler(servicio.Object);

            var resultado = await handler.Handle(
                new GetUsersQuery { Page = 2, PageSize = 20, Role = "Cliente" }, CancellationToken.None);

            resultado.Page.Should().Be(2);
            resultado.TotalRecords.Should().Be(25);
            resultado.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUsersQueryHandler_TrasladaLosFiltrosRecibidos()
        {
            var servicio = new Mock<IUserManagementService>();
            int pagina = 0, tamano = 0; string? rol = null;

            servicio.Setup(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                                                It.IsAny<CancellationToken>()))
                    .Callback<int, int, string?, CancellationToken>(
                        (p, t, r, _) => { pagina = p; tamano = t; rol = r; })
                    .ReturnsAsync(new PagedResult<UserInfoDto> { Data = new List<UserInfoDto>() });

            var handler = new GetUsersQueryHandler(servicio.Object);

            await handler.Handle(new GetUsersQuery { Page = 3, PageSize = 15, Role = "Cajero" },
                CancellationToken.None);

            pagina.Should().Be(3);
            tamano.Should().Be(15);
            rol.Should().Be("Cajero");
        }

        [Theory]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 21, false)]
        [InlineData(1, 20, true)]
        [InlineData(2, 10, true)]
        public void GetUsersQueryValidator_ValidaPaginacion(int page, int pageSize, bool esperado)
        {
            var validator = new GetUsersQueryValidator();

            var resultado = validator.Validate(new GetUsersQuery { Page = page, PageSize = pageSize });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("00112345678", "ana@correo.do", "Clave@12345!", "Clave@12345!", "Cliente", true)]
        [InlineData("", "ana@correo.do", "Clave@12345!", "Clave@12345!", "Cliente", false)]
        [InlineData("123", "ana@correo.do", "Clave@12345!", "Clave@12345!", "Cliente", false)]
        [InlineData("00112345678", "sin-arroba", "Clave@12345!", "Clave@12345!", "Cliente", false)]
        [InlineData("00112345678", "ana@correo.do", "corta", "corta", "Cliente", false)]
        [InlineData("00112345678", "ana@correo.do", "Clave@12345!", "Otra@12345!", "Cliente", false)]
        [InlineData("00112345678", "ana@correo.do", "Clave@12345!", "Clave@12345!", "Inventado", false)]
        public void CreateUserCommandValidator_ValidaCamposObligatorios(string cedula, string correo,
            string clave, string confirmacion, string rol, bool esperado)
        {
            var validator = new CreateUserCommandValidator();

            var resultado = validator.Validate(new CreateUserCommand
            {
                FirstName = "Ana",
                LastName = "Perez",
                Identification = cedula,
                Email = correo,
                UserName = correo,
                Password = clave,
                ConfirmPassword = confirmacion,
                Role = rol
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Fact]
        public void CreateUserCommandValidator_RechazaRolComercio()
        {
            var validator = new CreateUserCommandValidator();

            var resultado = validator.Validate(new CreateUserCommand
            {
                FirstName = "Tienda", LastName = "Central",
                Identification = "13012345678",
                Email = "tienda@correo.do", UserName = "tienda@correo.do",
                Password = "Clave@12345!", ConfirmPassword = "Clave@12345!",
                Role = "Comercio"
            });

            resultado.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("usuario-1", true)]
        [InlineData("", false)]
        public void SetUserStatusCommandValidator_ExigeUsuarioObjetivo(string objetivo, bool esperado)
        {
            var validator = new SetUserStatusCommandValidator();

            var resultado = validator.Validate(new SetUserStatusCommand
            {
                TargetUserId = objetivo, IsActive = true, CallerUserId = "admin-1"
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-3, false)]
        public void CreateCommerceUserCommandValidator_ExigeComercioValido(int comercioId, bool esperado)
        {
            var validator = new CreateCommerceUserCommandValidator();

            var resultado = validator.Validate(new CreateCommerceUserCommand
            {
                CommerceId = comercioId,
                FirstName = "Tienda", LastName = "Central",
                Identification = "13012345678",
                Email = "tienda@correo.do", UserName = "tienda@correo.do",
                Password = "Clave@12345!", ConfirmPassword = "Clave@12345!"
            });

            resultado.IsValid.Should().Be(esperado);
        }

        [Fact]
        public void UpdateUserCommandValidator_AdmiteContraseniaVacia()
        {
            var validator = new UpdateUserCommandValidator();

            var resultado = validator.Validate(new UpdateUserCommand
            {
                UserId = "usuario-1",
                FirstName = "Ana", LastName = "Perez",
                Identification = "00112345678",
                Email = "ana@correo.do", UserName = "ana@correo.do",
                Password = null, ConfirmPassword = null
            });

            resultado.IsValid.Should().BeTrue();
        }

        [Fact]
        public void UpdateUserCommandValidator_RechazaConfirmacionDistinta()
        {
            var validator = new UpdateUserCommandValidator();

            var resultado = validator.Validate(new UpdateUserCommand
            {
                UserId = "usuario-1",
                FirstName = "Ana", LastName = "Perez",
                Identification = "00112345678",
                Email = "ana@correo.do", UserName = "ana@correo.do",
                Password = "Clave@12345!", ConfirmPassword = "Distinta@12345!"
            });

            resultado.IsValid.Should().BeFalse();
        }
    }
}
