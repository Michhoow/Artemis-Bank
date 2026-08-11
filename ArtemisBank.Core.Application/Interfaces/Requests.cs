using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>Contexto del usuario que ejecuta la operacion financiera.</summary>
    public class OperationActor
    {
        public string? UserId { get; set; }
        public Roles? Role { get; set; }

        public static OperationActor Of(string? userId, Roles? role)
            => new OperationActor { UserId = userId, Role = role };
    }

    /// <summary>Movimiento de fondos entre dos cuentas de ahorro (express, beneficiario, terceros, propias).</summary>
    public class TransferRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; } = TransactionOperation.TransaccionExpress;
        public OperationActor Actor { get; set; } = new();
        /// <summary>Cuando aplica, id del cliente duenio de la cuenta de origen (validacion de propiedad).</summary>
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

    /// <summary>
    /// Credito externo hacia una cuenta de ahorro. Lo consumen Manuel (desembolso de prestamo,
    /// avance de efectivo, acreditacion de Hermes Pay) reutilizando la trazabilidad de Michael.
    /// </summary>
    public class ExternalCreditRequest
    {
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; }
        /// <summary>Texto que se mostrara en la columna Origen (numero de prestamo o ultimos 4 de tarjeta).</summary>
        public string OriginLabel { get; set; } = string.Empty;
        public OperationActor Actor { get; set; } = new();
    }

    /// <summary>Debito externo sobre una cuenta de ahorro registrado con trazabilidad completa.</summary>
    public class ExternalDebitRequest
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionOperation Operation { get; set; }
        public string BeneficiaryLabel { get; set; } = string.Empty;
        public OperationActor Actor { get; set; } = new();
    }
}
