namespace ArtemisBank.Core.Application.Dtos.Transactions
{
    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }

        public string TransactionType { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Beneficiary { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
