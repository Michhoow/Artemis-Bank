namespace ArtemisBank.Core.Application.Dtos.Common
{
    public class CreditCardInfoDto
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;

        public string LastFourDigits { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal Debt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool HasDebt => Debt > 0m;
        public string MaskedNumber => "**** **** **** " + LastFourDigits;
    }
}
