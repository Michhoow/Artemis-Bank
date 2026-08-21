using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class SavingsAccount : AuditableEntity
    {
        public string AccountNumber { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public AccountType Type { get; set; } = AccountType.Secundaria;

        public AccountStatus Status { get; set; } = AccountStatus.Activa;

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
        public ICollection<Beneficiary>? RegisteredAsBeneficiary { get; set; }

        public bool IsActive => Status == AccountStatus.Activa;
        public bool IsPrincipal => Type == AccountType.Principal;

        public string LastFourDigits =>
            string.IsNullOrEmpty(AccountNumber) || AccountNumber.Length < 4
                ? AccountNumber
                : AccountNumber.Substring(AccountNumber.Length - 4);
    }
}
