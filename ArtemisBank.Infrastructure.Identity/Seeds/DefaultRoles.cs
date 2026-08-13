using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Infrastructure.Identity.Seeds
{
    /// <summary>
    /// Crea los roles del sistema si aún no existen.
    /// WebApp: Administrador, Cajero, Cliente.
    /// WebApi: Administrador (compartido), Comercio.
    /// Diseñado para ser idempotente: puede ejecutarse varias veces sin duplicar datos.
    /// </summary>
    public static class DefaultRoles
    {
        // Nombres canónicos de rol — deben coincidir con ArtemisRoles.cs de la WebApp
        // y con los atributos [Authorize(Roles = "...")] de todos los controladores.
        public const string Administrador = "Administrador";
        public const string Cajero        = "Cajero";
        public const string Cliente       = "Cliente";
        public const string Comercio      = "Comercio";

        public static readonly IReadOnlyList<string> AllRoles =
            [Administrador, Cajero, Cliente, Comercio];

        /// <summary>
        /// Asegura que todos los roles existan en la base de datos.
        /// </summary>
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            foreach (var roleName in AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                        logger.LogInformation("Rol '{Role}' creado por seeding.", roleName);
                    else
                        logger.LogWarning("No se pudo crear el rol '{Role}': {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
