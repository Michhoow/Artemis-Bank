namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    public class SavingsAccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsPrincipal =>
            string.Equals(Type, "Principal", StringComparison.OrdinalIgnoreCase);

        public bool IsActive =>
            string.Equals(Status, "Activa", StringComparison.OrdinalIgnoreCase);

        public string LastFourDigits =>
            string.IsNullOrEmpty(AccountNumber) || AccountNumber.Length < 4
                ? AccountNumber
                : AccountNumber.Substring(AccountNumber.Length - 4);
    }
}
