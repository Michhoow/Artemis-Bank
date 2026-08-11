using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Application.Common.Constants
{
    /// <summary>Textos fijos exigidos por el documento funcional para Origen / Beneficiario y etiquetas.</summary>
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
    }
}
