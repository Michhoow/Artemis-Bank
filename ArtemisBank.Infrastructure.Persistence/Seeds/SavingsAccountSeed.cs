using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Seeds
{
    /// <summary>
    /// Datos minimos funcionales para poder probar los modulos de cuentas, transacciones y cajero
    /// antes de que el modulo de usuarios este integrado.
    ///
    /// Los ClientId coinciden con DemoDirectory de la capa Application.
    /// Cuando Monserrat integre Identity, este seed debe alinearse con los Id reales
    /// de los usuarios creados por su propio seeding (o eliminarse).
    ///
    /// Se ejecuta solo si la configuracion SeedDemoData es true y la tabla esta vacia.
    /// </summary>
    public static class SavingsAccountSeed
    {
        public static async Task RunAsync(ArtemisDbContext context)
        {
            if (await context.SavingsAccounts.AnyAsync()) return;

            var now = DateTime.Now;

            var accounts = new List<SavingsAccount>
            {
                new SavingsAccount
                {
                    AccountNumber = "100000001", ClientId = "demo-cliente-0001", Balance = 17500.00m,
                    Type = AccountType.Principal, Status = AccountStatus.Activa,
                    CreatedAt = now.AddDays(-30), CreatedByUserId = "demo-admin-0001"
                },
                new SavingsAccount
                {
                    AccountNumber = "100000002", ClientId = "demo-cliente-0001", Balance = 5000.00m,
                    Type = AccountType.Secundaria, Status = AccountStatus.Activa,
                    CreatedAt = now.AddDays(-10), CreatedByUserId = "demo-admin-0001"
                },
                new SavingsAccount
                {
                    AccountNumber = "100000003", ClientId = "demo-cliente-0002", Balance = 9800.50m,
                    Type = AccountType.Principal, Status = AccountStatus.Activa,
                    CreatedAt = now.AddDays(-25), CreatedByUserId = "demo-admin-0001"
                },
                new SavingsAccount
                {
                    AccountNumber = "100000004", ClientId = "demo-cliente-0003", Balance = 0.00m,
                    Type = AccountType.Principal, Status = AccountStatus.Activa,
                    CreatedAt = now.AddDays(-20), CreatedByUserId = "demo-admin-0001"
                }
            };

            await context.SavingsAccounts.AddRangeAsync(accounts);
            await context.SaveChangesAsync();

            // Un credito inicial por cuenta con balance, para que el historial no nazca vacio.
            var seedTransactions = accounts
                .Where(a => a.Balance > 0m)
                .Select(a => new Transaction
                {
                    SavingsAccountId = a.Id,
                    Amount = a.Balance,
                    Type = TransactionType.Credito,
                    Status = TransactionStatus.Aprobada,
                    Operation = TransactionOperation.BalanceInicial,
                    Origin = "DEPÓSITO",
                    Beneficiary = a.AccountNumber,
                    OperationReference = Guid.NewGuid().ToString("N"),
                    PerformedByUserId = "demo-admin-0001",
                    PerformedByRole = Roles.Administrador,
                    CreatedAt = a.CreatedAt,
                    CreatedByUserId = "demo-admin-0001"
                })
                .ToList();

            await context.Transactions.AddRangeAsync(seedTransactions);
            await context.SaveChangesAsync();
        }
    }
}
