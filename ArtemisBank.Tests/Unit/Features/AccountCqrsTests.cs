using ArtemisBank.Core.Application.Features.Account.Commands;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Features
{
    public class AccountCqrsTests
    {
        [Fact]
        public async Task LoginCommandHandler_CredencialesValidas_DevuelveElToken()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.LoginAsync("admin@artemisbank.do", "Admin@12345!"))
                    .ReturnsAsync(("jwt-de-prueba", null));

            var handler = new LoginCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new LoginCommand
            {
                UserName = "admin@artemisbank.do",
                Password = "Admin@12345!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
            resultado.Token.Should().Be("jwt-de-prueba");
            resultado.Error.Should().BeNull();
        }

        [Fact]
        public async Task LoginCommandHandler_CredencialesInvalidas_NoDevuelveToken()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((null, "Los datos de acceso son inválidos."));

            var handler = new LoginCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new LoginCommand
            {
                UserName = "noexiste@correo.do",
                Password = "Mala@12345!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Token.Should().BeNull();
            resultado.Error.Should().Contain("inválidos");
        }

        [Fact]
        public async Task LoginCommandHandler_CuentaInactiva_PropagaElMotivo()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((null, "Su cuenta se encuentra inactiva."));

            var handler = new LoginCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new LoginCommand
            {
                UserName = "inactivo@correo.do",
                Password = "Clave@12345!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Error.Should().Contain("inactiva");
        }

        [Fact]
        public async Task ConfirmAccountCommandHandler_TokenValido_ActivaLaCuenta()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.ConfirmAccountAsync("usuario-1", "token-valido"))
                    .ReturnsAsync((true, null));

            var handler = new ConfirmAccountCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new ConfirmAccountCommand
            {
                UserId = "usuario-1",
                Token = "token-valido"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ConfirmAccountCommandHandler_TokenYaUsado_SeRechaza()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.ConfirmAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((false, "El enlace de activación no es válido o ya fue utilizado."));

            var handler = new ConfirmAccountCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new ConfirmAccountCommand
            {
                UserId = "usuario-1",
                Token = "token-ya-usado"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetResetTokenCommandHandler_CorreoRegistrado_DevuelveTokenYUsuario()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.GetResetTokenAsync("ana@correo.do"))
                    .ReturnsAsync(("token-reset", "usuario-1", null));

            var handler = new GetResetTokenCommandHandler(servicio.Object);

            var resultado = await handler.Handle(
                new GetResetTokenCommand { Email = "ana@correo.do" }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
            resultado.Token.Should().Be("token-reset");
            resultado.UserId.Should().Be("usuario-1");
        }

        [Fact]
        public async Task GetResetTokenCommandHandler_CorreoDesconocido_NoRevelaSiExiste()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.GetResetTokenAsync(It.IsAny<string>()))
                    .ReturnsAsync((null, null, null));

            var handler = new GetResetTokenCommandHandler(servicio.Object);

            var resultado = await handler.Handle(
                new GetResetTokenCommand { Email = "desconocido@correo.do" }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Token.Should().BeNull();
        }

        [Fact]
        public async Task ResetPasswordCommandHandler_TokenValido_CambiaLaContrasenia()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.ResetPasswordAsync("usuario-1", "token-valido", "NuevaClave@123!"))
                    .ReturnsAsync((true, null));

            var handler = new ResetPasswordCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new ResetPasswordCommand
            {
                UserId = "usuario-1",
                Token = "token-valido",
                NewPassword = "NuevaClave@123!",
                ConfirmPassword = "NuevaClave@123!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ResetPasswordCommandHandler_TokenExpirado_SeRechaza()
        {
            var servicio = new Mock<IAccountAuthService>();
            servicio.Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((false, "El enlace o código utilizado no es válido o ya expiró."));

            var handler = new ResetPasswordCommandHandler(servicio.Object);

            var resultado = await handler.Handle(new ResetPasswordCommand
            {
                UserId = "usuario-1",
                Token = "token-expirado",
                NewPassword = "NuevaClave@123!",
                ConfirmPassword = "NuevaClave@123!"
            }, CancellationToken.None);

            resultado.Succeeded.Should().BeFalse();
            resultado.Error.Should().Contain("expiró");
        }

        [Theory]
        [InlineData("admin@artemisbank.do", "Admin@12345!", true)]
        [InlineData("", "Admin@12345!", false)]
        [InlineData("admin@artemisbank.do", "", false)]
        [InlineData("", "", false)]
        public void LoginCommandValidator_ExigeUsuarioYContrasenia(string usuario, string clave, bool esperado)
        {
            var validator = new LoginCommandValidator();

            var resultado = validator.Validate(new LoginCommand { UserName = usuario, Password = clave });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("usuario-1", "token", true)]
        [InlineData("", "token", false)]
        [InlineData("usuario-1", "", false)]
        public void ConfirmAccountCommandValidator_ExigeUsuarioYToken(string usuario, string token, bool esperado)
        {
            var validator = new ConfirmAccountCommandValidator();

            var resultado = validator.Validate(new ConfirmAccountCommand { UserId = usuario, Token = token });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("ana@correo.do", true)]
        [InlineData("sin-arroba", false)]
        [InlineData("", false)]
        public void GetResetTokenCommandValidator_ValidaElCorreo(string correo, bool esperado)
        {
            var validator = new GetResetTokenCommandValidator();

            var resultado = validator.Validate(new GetResetTokenCommand { Email = correo });

            resultado.IsValid.Should().Be(esperado);
        }

        [Theory]
        [InlineData("usuario-1", "token", "NuevaClave@123!", "NuevaClave@123!", true)]
        [InlineData("", "token", "NuevaClave@123!", "NuevaClave@123!", false)]
        [InlineData("usuario-1", "", "NuevaClave@123!", "NuevaClave@123!", false)]
        [InlineData("usuario-1", "token", "corta", "corta", false)]
        [InlineData("usuario-1", "token", "NuevaClave@123!", "Distinta@123!", false)]
        public void ResetPasswordCommandValidator_ValidaTokenYConfirmacion(string usuario, string token,
            string clave, string confirmacion, bool esperado)
        {
            var validator = new ResetPasswordCommandValidator();

            var resultado = validator.Validate(new ResetPasswordCommand
            {
                UserId = usuario,
                Token = token,
                NewPassword = clave,
                ConfirmPassword = confirmacion
            });

            resultado.IsValid.Should().Be(esperado);
        }
    }
}
