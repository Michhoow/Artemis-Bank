using ArtemisBank.Core.Domain.Common;

namespace ArtemisBank.Core.Domain.Entities
{
    public class Beneficiary : AuditableEntity
    {
        public string ClientId { get; set; } = string.Empty;

        public int SavingsAccountId { get; set; }

        public SavingsAccount? SavingsAccount { get; set; }
    }
}
