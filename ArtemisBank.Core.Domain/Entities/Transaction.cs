using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class Transaction : AuditableEntity
    {
        public int SavingsAccountId { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Aprobada;

        public TransactionOperation Operation { get; set; }

        public string Origin { get; set; } = string.Empty;

        public string Beneficiary { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public string OperationReference { get; set; } = string.Empty;

        public string? PerformedByUserId { get; set; }

        public Roles? PerformedByRole { get; set; }

        public SavingsAccount? SavingsAccount { get; set; }

        public bool IsPayment =>
            Operation == TransactionOperation.PagoTarjeta || Operation == TransactionOperation.PagoPrestamo;
    }
}
