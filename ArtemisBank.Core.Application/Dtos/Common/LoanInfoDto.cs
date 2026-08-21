namespace ArtemisBank.Core.Application.Dtos.Common
{
    public class LoanInfoDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public decimal ApprovedCapital { get; set; }
        public decimal PendingAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public bool IsActive { get; set; }
        public bool IsOverdue { get; set; }

        public bool HasPendingInstallments => IsActive && PendingAmount > 0m;
    }
}
