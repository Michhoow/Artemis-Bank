using ArtemisBank.Infrastructure.Identity.Common;
using ArtemisBank.Infrastructure.Identity.Contexts;
using ArtemisBank.Infrastructure.Identity.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ArtemisBank.Tests.Integration
{
    public class IdentityRepositoryIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _conexion;
        private readonly ServiceProvider _proveedor;

        public IdentityRepositoryIntegrationTests()
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();

            var servicios = new ServiceCollection();

            servicios.AddLogging();

            servicios.AddDataProtection();

            servicios.AddDbContext<IdentityContext>(opciones =>
                opciones.UseSqlite(_conexion));

            servicios.AddIdentityCore<AppUser>(opciones =>
                    {
                        opciones.Password.RequiredLength = 8;
                        opciones.Password.RequireDigit = true;
                        opciones.Password.RequireLowercase = true;
                        opciones.Password.RequireUppercase = true;
                        opciones.Password.RequireNonAlphanumeric = true;
                        opciones.User.RequireUniqueEmail = true;
                    })
                .AddRoles<IdentityRole>()
                .AddErrorDescriber<SpanishIdentityErrorDescriber>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

            _proveedor = servicios.BuildServiceProvider();

            var contexto = _proveedor.GetRequiredService<IdentityContext>();
            contexto.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _proveedor.Dispose();
            _conexion.Dispose();
            GC.SuppressFinalize(this);
        }

        private UserManager<AppUser> Usuarios => _proveedor.GetRequiredService<UserManager<AppUser>>();
        private RoleManager<IdentityRole> Roles => _proveedor.GetRequiredService<RoleManager<IdentityRole>>();

        private static AppUser NuevoUsuario(string correo, string cedula) => new()
        {
            UserName = correo,
            Email = correo,
            FirstName = "Nombre",
            LastName = "Apellido",
            Identification = cedula,
            IsActive = false,
            EmailConfirmed = false
        };

        [Fact]
        public async Task UserManager_PersisteElUsuarioConSusDatosPropios()
        {
            var creado = await Usuarios.CreateAsync(
                NuevoUsuario("ana@correo.do", "00100000101"), "Clave@12345!");

            creado.Succeeded.Should().BeTrue();

            var recuperado = await Usuarios.FindByNameAsync("ana@correo.do");

            recuperado.Should().NotBeNull();
            recuperado!.Identification.Should().Be("00100000101");
            recuperado.FirstName.Should().Be("Nombre");

            recuperado.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_NuncaGuardaLaContraseniaEnTextoPlano()
        {
            await Usuarios.CreateAsync(NuevoUsuario("luis@correo.do", "00100000102"), "Clave@12345!");

            var recuperado = await Usuarios.FindByNameAsync("luis@correo.do");

            recuperado!.PasswordHash.Should().NotBeNullOrWhiteSpace();
            recuperado.PasswordHash.Should().NotContain("Clave@12345!");
        }

        [Fact]
        public async Task UserManager_ValidaLaContraseniaContraElHashAlmacenado()
        {
            await Usuarios.CreateAsync(NuevoUsuario("maria@correo.do", "00100000103"), "Clave@12345!");

            var usuario = await Usuarios.FindByNameAsync("maria@correo.do");

            (await Usuarios.CheckPasswordAsync(usuario!, "Clave@12345!")).Should().BeTrue();
            (await Usuarios.CheckPasswordAsync(usuario!, "Incorrecta@123!")).Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_RechazaNombreDeUsuarioDuplicado()
        {
            await Usuarios.CreateAsync(NuevoUsuario("repetido@correo.do", "00100000104"), "Clave@12345!");

            var segundo = NuevoUsuario("repetido@correo.do", "00100000105");
            var resultado = await Usuarios.CreateAsync(segundo, "Clave@12345!");

            resultado.Succeeded.Should().BeFalse();
            resultado.Errors.Should().Contain(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
        }

        [Fact]
        public async Task UserManager_RechazaCorreoDuplicado()
        {
            await Usuarios.CreateAsync(NuevoUsuario("correo@correo.do", "00100000106"), "Clave@12345!");

            var segundo = NuevoUsuario("otro-usuario@correo.do", "00100000107");
            segundo.Email = "correo@correo.do";

            var resultado = await Usuarios.CreateAsync(segundo, "Clave@12345!");

            resultado.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_ElIndiceUnicoImpideRepetirLaCedula()
        {
            await Usuarios.CreateAsync(NuevoUsuario("uno@correo.do", "00100000108"), "Clave@12345!");

            var accion = async () => await Usuarios.CreateAsync(
                NuevoUsuario("dos@correo.do", "00100000108"), "Clave@12345!");

            await accion.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task UserManager_LosErroresDePoliticaLleganEnEspaniol()
        {
            var resultado = await Usuarios.CreateAsync(
                NuevoUsuario("debil@correo.do", "00100000109"), "corta");

            resultado.Succeeded.Should().BeFalse();
            resultado.Errors.Should().NotBeEmpty();

            resultado.Errors.Should().OnlyContain(e => !e.Description.Contains("Passwords must"));
        }

        [Fact]
        public async Task UserManager_ActualizaElEstadoDelUsuario()
        {
            await Usuarios.CreateAsync(NuevoUsuario("activar@correo.do", "00100000110"), "Clave@12345!");

            var usuario = await Usuarios.FindByNameAsync("activar@correo.do");
            usuario!.IsActive = true;
            usuario.EmailConfirmed = true;

            var actualizado = await Usuarios.UpdateAsync(usuario);
            actualizado.Succeeded.Should().BeTrue();

            var recuperado = await Usuarios.FindByNameAsync("activar@correo.do");
            recuperado!.IsActive.Should().BeTrue();
            recuperado.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public async Task UserManager_EncuentraAlUsuarioPorSuCorreo()
        {
            await Usuarios.CreateAsync(NuevoUsuario("buscar@correo.do", "00100000111"), "Clave@12345!");

            var porCorreo = await Usuarios.FindByEmailAsync("buscar@correo.do");

            porCorreo.Should().NotBeNull();
            porCorreo!.Identification.Should().Be("00100000111");
        }

        [Fact]
        public async Task RoleManager_CreaYRecuperaLosRolesDelSistema()
        {
            foreach (var rol in new[] { "Administrador", "Cajero", "Cliente", "Comercio" })
            {
                (await Roles.CreateAsync(new IdentityRole(rol))).Succeeded.Should().BeTrue();
            }

            (await Roles.RoleExistsAsync("Administrador")).Should().BeTrue();
            (await Roles.RoleExistsAsync("Inventado")).Should().BeFalse();

            Roles.Roles.Count().Should().Be(4);
        }

        [Fact]
        public async Task RoleManager_RechazaUnRolDuplicado()
        {
            await Roles.CreateAsync(new IdentityRole("Cajero"));

            var resultado = await Roles.CreateAsync(new IdentityRole("Cajero"));

            resultado.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_AsignaElRolYLoPersiste()
        {
            await Roles.CreateAsync(new IdentityRole("Cliente"));
            await Usuarios.CreateAsync(NuevoUsuario("conrol@correo.do", "00100000112"), "Clave@12345!");

            var usuario = await Usuarios.FindByNameAsync("conrol@correo.do");

            var asignado = await Usuarios.AddToRoleAsync(usuario!, "Cliente");
            asignado.Succeeded.Should().BeTrue();

            var roles = await Usuarios.GetRolesAsync(usuario!);

            roles.Should().ContainSingle().Which.Should().Be("Cliente");
            (await Usuarios.IsInRoleAsync(usuario!, "Cliente")).Should().BeTrue();
            (await Usuarios.IsInRoleAsync(usuario!, "Administrador")).Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_ListaLosUsuariosDeUnRol()
        {
            await Roles.CreateAsync(new IdentityRole("Cajero"));

            for (var i = 1; i <= 2; i++)
            {
                await Usuarios.CreateAsync(
                    NuevoUsuario($"cajero{i}@correo.do", $"0010000012{i}"), "Clave@12345!");
                var creado = await Usuarios.FindByNameAsync($"cajero{i}@correo.do");
                await Usuarios.AddToRoleAsync(creado!, "Cajero");
            }

            var delRol = await Usuarios.GetUsersInRoleAsync("Cajero");

            delRol.Should().HaveCount(2);
        }

        [Fact]
        public async Task UserManager_GeneraTokenDeConfirmacionQueValidaLaCuenta()
        {
            await Usuarios.CreateAsync(NuevoUsuario("token@correo.do", "00100000130"), "Clave@12345!");
            var usuario = await Usuarios.FindByNameAsync("token@correo.do");

            var token = await Usuarios.GenerateEmailConfirmationTokenAsync(usuario!);
            token.Should().NotBeNullOrWhiteSpace();

            var confirmado = await Usuarios.ConfirmEmailAsync(usuario!, token);

            confirmado.Succeeded.Should().BeTrue();
            (await Usuarios.IsEmailConfirmedAsync(usuario!)).Should().BeTrue();
        }

        [Fact]
        public async Task UserManager_ElTokenDeConfirmacionDejaDeValerAlCambiarElSecurityStamp()
        {
            await Usuarios.CreateAsync(NuevoUsuario("unsolo@correo.do", "00100000131"), "Clave@12345!");
            var usuario = await Usuarios.FindByNameAsync("unsolo@correo.do");

            var token = await Usuarios.GenerateEmailConfirmationTokenAsync(usuario!);
            (await Usuarios.ConfirmEmailAsync(usuario!, token)).Succeeded.Should().BeTrue();

            await Usuarios.UpdateSecurityStampAsync(usuario!);

            var recargado = await Usuarios.FindByNameAsync("unsolo@correo.do");
            var conTokenViejo = await Usuarios.ConfirmEmailAsync(recargado!, token);

            conTokenViejo.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_RechazaUnTokenDeConfirmacionInventado()
        {
            await Usuarios.CreateAsync(NuevoUsuario("falso@correo.do", "00100000132"), "Clave@12345!");
            var usuario = await Usuarios.FindByNameAsync("falso@correo.do");

            var resultado = await Usuarios.ConfirmEmailAsync(usuario!, "token-inventado");

            resultado.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_TokenDeRestablecimientoCambiaLaContrasenia()
        {
            await Usuarios.CreateAsync(NuevoUsuario("reset@correo.do", "00100000133"), "Clave@12345!");
            var usuario = await Usuarios.FindByNameAsync("reset@correo.do");

            var token = await Usuarios.GeneratePasswordResetTokenAsync(usuario!);
            var resultado = await Usuarios.ResetPasswordAsync(usuario!, token, "NuevaClave@123!");

            resultado.Succeeded.Should().BeTrue();

            var actualizado = await Usuarios.FindByNameAsync("reset@correo.do");
            (await Usuarios.CheckPasswordAsync(actualizado!, "NuevaClave@123!")).Should().BeTrue();
            (await Usuarios.CheckPasswordAsync(actualizado!, "Clave@12345!")).Should().BeFalse();
        }

        [Fact]
        public async Task UserManager_ElTokenDeRestablecimientoNoSirveDosVeces()
        {
            await Usuarios.CreateAsync(NuevoUsuario("reset2@correo.do", "00100000134"), "Clave@12345!");
            var usuario = await Usuarios.FindByNameAsync("reset2@correo.do");

            var token = await Usuarios.GeneratePasswordResetTokenAsync(usuario!);
            await Usuarios.ResetPasswordAsync(usuario!, token, "NuevaClave@123!");

            var recargado = await Usuarios.FindByNameAsync("reset2@correo.do");
            var reintento = await Usuarios.ResetPasswordAsync(recargado!, token, "OtraClave@123!");

            reintento.Succeeded.Should().BeFalse();
        }
    }
}
