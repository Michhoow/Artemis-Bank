using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Application.Common.Constants
{
    public static class DisplayText
    {
        public const string Deposit = "DEPÓSITO";
        public const string Withdrawal = "RETIRO";
        public const string CashAdvance = "AVANCE";

        public const string Debit = "DÉBITO";
        public const string Credit = "CRÉDITO";
        public const string Approved = "APROBADA";
        public const string Rejected = "RECHAZADA";

        public static string Type(TransactionType type) => type == TransactionType.Debito ? Debit : Credit;

        public static string Status(TransactionStatus status)
            => status == TransactionStatus.Aprobada ? Approved : Rejected;

        public static string AccountType(AccountType type)
            => type == Domain.Common.Enums.AccountType.Principal ? "Principal" : "Secundaria";

        public static string AccountStatus(AccountStatus status)
            => status == Domain.Common.Enums.AccountStatus.Activa ? "Activa" : "Cancelada";

        public const string LoanUpToDate = "Al día";
        public const string LoanOverdue = "En mora";

        public const string InstallmentPending = "Pendiente";
        public const string InstallmentPartiallyPaid = "Parcialmente pagada";
        public const string InstallmentPaid = "Pagada";

        public static string LoanStatus(LoanStatus status)
            => status == Domain.Common.Enums.LoanStatus.Activo ? "Activo" : "Completado";

        public const string ConsumptionApproved = "APROBADO";
        public const string ConsumptionRejected = "RECHAZADO";

        public static string CardStatus(CardStatus status)
            => status == Domain.Common.Enums.CardStatus.Activa ? "Activa" : "Cancelada";

        public static string ConsumptionStatus(ConsumptionStatus status)
            => status == Domain.Common.Enums.ConsumptionStatus.Aprobado ? ConsumptionApproved : ConsumptionRejected;

        public static string ConsumptionType(ConsumptionType type) => type switch
        {
            Domain.Common.Enums.ConsumptionType.AvanceEfectivo => "Avance de efectivo",
            Domain.Common.Enums.ConsumptionType.InteresAvance => "Interés por avance de efectivo",
            Domain.Common.Enums.ConsumptionType.Pago => "Pago a tarjeta",
            _ => "Consumo"
        };
    }
}
