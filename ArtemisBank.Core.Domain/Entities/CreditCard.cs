using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class CreditCard : AuditableEntity
    {
        public string CardNumber { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public decimal CreditLimit { get; set; }

        public decimal Debt { get; set; }

        public int ExpirationMonth { get; set; }

        public int ExpirationYear { get; set; }

        public string CvcHash { get; set; } = string.Empty;

        public string CvcSalt { get; set; } = string.Empty;

        public CardStatus Status { get; set; } = CardStatus.Activa;

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ICollection<CardConsumption>? Consumptions { get; set; }

        public bool IsActive => Status == CardStatus.Activa;

        public decimal AvailableCredit => Math.Max(0m, CreditLimit - Debt);

        public bool HasDebt => Debt > 0m;

        public string LastFourDigits =>
            string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 4
                ? CardNumber
                : CardNumber.Substring(CardNumber.Length - 4);

        public string ExpirationDisplay =>
            $"{ExpirationMonth:00}/{(ExpirationYear % 100):00}";

        public bool IsExpired(DateTime? reference = null)
        {
            var now = reference ?? DateTime.Now;
            var lastDay = new DateTime(ExpirationYear, ExpirationMonth,
                DateTime.DaysInMonth(ExpirationYear, ExpirationMonth), 23, 59, 59);
            return now > lastDay;
        }
    }
}
