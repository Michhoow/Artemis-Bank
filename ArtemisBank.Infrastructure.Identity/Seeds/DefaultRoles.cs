using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Infrastructure.Identity.Seeds
{
    public static class DefaultRoles
    {
        public const string Administrador = "Administrador";
        public const string Cajero        = "Cajero";
        public const string Cliente       = "Cliente";
        public const string Comercio      = "Comercio";

        public static readonly IReadOnlyList<string> AllRoles =
            [Administrador, Cajero, Cliente, Comercio];

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
