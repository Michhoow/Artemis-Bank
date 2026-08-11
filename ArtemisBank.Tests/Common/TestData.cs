using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Contexts;

namespace ArtemisBank.Tests.Common
{
    public static class TestData
    {
        public const string ClientOne = "cliente-1";
        public const string ClientTwo = "cliente-2";
        public const string AdminId = "admin-1";
        public const string CashierId = "cajero-1";

        public static SavingsAccount Account(string number, string clientId, decimal balance,
            AccountType type = AccountType.Secundaria, AccountStatus status = AccountStatus.Activa)
            => new SavingsAccount
            {
                AccountNumber = number,
                ClientId = clientId,
                Balance = balance,
                Type = type,
                Status = status,
                CreatedAt = DateTime.Now,
                CreatedByUserId = AdminId
            };

        public static UserInfoDto User(string id, string identification, bool isActive = true,
            string role = "Cliente")
            => new UserInfoDto
            {
                Id = id,
                FirstName = "Nombre",
                LastName = "Apellido",
                Identification = identification,
                Email = $"{id}@artemisbank.do",
                UserName = id,
                Role = role,
                IsActive = isActive
            };

        /// <summary>Escenario base: cliente 1 con principal y secundaria; cliente 2 con principal.</summary>
        public static async Task<(SavingsAccount Principal, SavingsAccount Secondary, SavingsAccount Other)>
            SeedAsync(ArtemisDbContext context)
        {
            var principal = Account("100000001", ClientOne, 10000m, AccountType.Principal);
            var secondary = Account("100000002", ClientOne, 2500m);
            var other = Account("200000001", ClientTwo, 500m, AccountType.Principal);

            context.SavingsAccounts.AddRange(principal, secondary, other);
            await context.SaveChangesAsync();

            return (principal, secondary, other);
        }
    }
}
