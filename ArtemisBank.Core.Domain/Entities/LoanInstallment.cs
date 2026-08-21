using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class LoanInstallment : AuditableEntity
    {
        public int LoanId { get; set; }

        public int Number { get; set; }

        public DateTime DueDate { get; set; }

        public decimal CapitalAmount { get; set; }

        public decimal InterestAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingCapital { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pendiente;

        public bool IsOverdue { get; set; }

        public DateTime? PaidAt { get; set; }

        public Loan? Loan { get; set; }

        public decimal PendingAmount => Math.Max(0m, TotalAmount - PaidAmount);

        public bool IsSettled => PendingAmount <= 0m;
    }
}
