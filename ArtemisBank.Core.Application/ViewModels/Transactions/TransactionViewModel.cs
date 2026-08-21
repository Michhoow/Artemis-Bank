namespace ArtemisBank.Core.Application.ViewModels.Transactions
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Beneficiary { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public bool IsCredit { get; set; }
        public bool IsApproved { get; set; }
    }
}
