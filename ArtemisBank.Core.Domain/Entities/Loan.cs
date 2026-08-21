using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class Loan : AuditableEntity
    {
        public string LoanNumber { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public decimal ApprovedCapital { get; set; }

        public decimal AnnualInterestRate { get; set; }

        public int TermInMonths { get; set; }

        public decimal MonthlyInstallment { get; set; }

        public decimal TotalToPay { get; set; }

        public decimal PaidAmount { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Activo;

        public string DisbursementAccountNumber { get; set; } = string.Empty;

        public DateTime FirstDueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public ICollection<LoanInstallment>? Installments { get; set; }

        public bool IsActive => Status == LoanStatus.Activo;

        public decimal PendingAmount =>
            Installments == null
                ? Math.Max(0m, TotalToPay - PaidAmount)
                : Math.Max(0m, Installments.Sum(i => i.TotalAmount - i.PaidAmount));

        public int TotalInstallments => Installments?.Count ?? TermInMonths;

        public int PaidInstallments =>
            Installments?.Count(i => i.Status == InstallmentStatus.Pagada) ?? 0;

        public bool IsOverdue =>
            Installments != null && Installments.Any(i => i.IsOverdue && !i.IsSettled);
    }
}
