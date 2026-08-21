using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Application.Interfaces
{
    public class OperationActor
    {
        public string? UserId { get; set; }
        public Roles? Role { get; set; }

        public static OperationActor Of(string? userId, Roles? role)
            => new OperationActor { UserId = userId, Role = role };
    }

    public class TransferRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; } = TransactionOperation.TransaccionExpress;
        public OperationActor Actor { get; set; } = new();

        public string? ExpectedSourceOwnerId { get; set; }
        public bool NotifyByEmail { get; set; } = true;
    }

    public class DepositRequest
    {
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public OperationActor Actor { get; set; } = new();
    }

    public class WithdrawalRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public OperationActor Actor { get; set; } = new();
    }

    public class CardPaymentRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public int CreditCardId { get; set; }
        public decimal Amount { get; set; }
        public OperationActor Actor { get; set; } = new();
        public string? ExpectedSourceOwnerId { get; set; }
    }

    public class LoanPaymentRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
        public OperationActor Actor { get; set; } = new();
        public string? ExpectedSourceOwnerId { get; set; }
    }

    public class ExternalCreditRequest
    {
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; }

        public string OriginLabel { get; set; } = string.Empty;
        public OperationActor Actor { get; set; } = new();
    }

    public class ExternalDebitRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; }
        public string BeneficiaryLabel { get; set; } = string.Empty;
        public OperationActor Actor { get; set; } = new();
    }
}
