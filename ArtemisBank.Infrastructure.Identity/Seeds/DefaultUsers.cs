using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Infrastructure.Identity.Seeds
{
    public static class DefaultUsers
    {
        private static readonly AppUser AdminUser = new()
        {
            UserName       = "admin@artemisbank.do",
            Email          = "admin@artemisbank.do",
            FirstName      = "Admin",
            LastName       = "Artemis",
            Identification = "00100000001",
            IsActive       = true,
            EmailConfirmed = true
        };
        private const string AdminPassword = "Admin@12345!";

        private static readonly AppUser CajeroUser = new()
        {
            UserName       = "cajero@artemisbank.do",
            Email          = "cajero@artemisbank.do",
            FirstName      = "Cajero",
            LastName       = "Default",
            Identification = "00100000002",
            IsActive       = true,
            EmailConfirmed = true
        };
        private const string CajeroPassword = "Cajero@12345!";

        private static readonly AppUser ClienteUser = new()
        {
            UserName       = "cliente@artemisbank.do",
            Email          = "cliente@artemisbank.do",
            FirstName      = "Cliente",
            LastName       = "Default",
            Identification = "00100000003",
            IsActive       = true,
            EmailConfirmed = true
        };
        private const string ClientePassword = "Cliente@12345!";

        private static readonly AppUser ComercioUser = new()
        {
            UserName       = "comercio@artemisbank.do",
            Email          = "comercio@artemisbank.do",
            FirstName      = "Comercio",
            LastName       = "Default",
            Identification = "13000000001",
            IsActive       = true,
            EmailConfirmed = true
        };
        private const string ComercioPassword = "Comercio@12345!";

        public static async Task SeedAsync(UserManager<AppUser> userManager,
            ILogger logger, IServiceProvider serviceProvider)
        {
            await CreateIfNotExistsAsync(userManager, AdminUser,   AdminPassword,   DefaultRoles.Administrador, logger, serviceProvider);
            await CreateIfNotExistsAsync(userManager, CajeroUser,  CajeroPassword,  DefaultRoles.Cajero,        logger, serviceProvider);
            await CreateIfNotExistsAsync(userManager, ClienteUser, ClientePassword, DefaultRoles.Cliente,       logger, serviceProvider);
            await CreateIfNotExistsAsync(userManager, ComercioUser,ComercioPassword,DefaultRoles.Comercio,      logger, serviceProvider);
        }

        private static async Task CreateIfNotExistsAsync(
            UserManager<AppUser> userManager,
            AppUser user,
            string password,
            string role,
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            var existing = await userManager.FindByNameAsync(user.UserName!);
            if (existing is not null) return;

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogWarning("Seed: no se pudo crear el usuario '{User}': {Errors}",
                    user.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Seed: usuario '{User}' creado y asignado al rol '{Role}'.",
                    user.UserName, role);

                if (role == DefaultRoles.Cliente || role == DefaultRoles.Comercio)
                {
                    try
                    {
                        var accountGen = serviceProvider.GetService<IAccountNumberGenerator>();
                        var accountRepo = serviceProvider.GetService<ISavingsAccountRepository>();
                        var commerceRepo = serviceProvider.GetService<ICommerceRepository>();

                        if (accountGen != null && accountRepo != null)
                        {
                            var hasPrincipal = (await accountRepo.GetPrincipalByClientAsync(user.Id)) != null;
                            if (!hasPrincipal)
                            {
                                var accNum = await accountGen.GenerateAsync();
                                await accountRepo.AddAsync(new SavingsAccount
                                {
                                    AccountNumber   = accNum,
                                    ClientId        = user.Id,
                                    Balance         = 1000.00m,
                                    Type            = AccountType.Principal,
                                    Status          = AccountStatus.Activa,
                                    CreatedAt       = DateTime.Now,
                                    CreatedByUserId = user.Id
                                });

                                if (role == DefaultRoles.Comercio && commerceRepo != null)
                                {
                                    await commerceRepo.AddAsync(new Commerce
                                    {
                                        Name          = $"{user.FirstName} {user.LastName}".Trim(),
                                        Rnc           = user.Identification,
                                        Email         = user.Email ?? string.Empty,
                                        Phone         = "8095550000",
                                        UserId        = user.Id,
                                        AccountNumber = accNum,
                                        IsActive      = true,
                                        CreatedAt     = DateTime.Now,
                                        CreatedByUserId = user.Id
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Seed: no se pudo generar la cuenta principal para '{User}'.", user.UserName);
                    }
                }
            }
            else
            {
                logger.LogWarning("Seed: usuario '{User}' creado pero no se asignó al rol '{Role}': {Errors}",
                    user.UserName, role,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
