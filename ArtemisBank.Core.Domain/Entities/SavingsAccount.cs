using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Cuenta de ahorro. Contenedor de saldo de todo el sistema.
    /// La cuenta Principal se crea automaticamente al registrar el cliente (modulo de usuarios).
    /// Desde el modulo de cuentas de ahorro solo se crean cuentas Secundarias.
    /// </summary>
    public class SavingsAccount : AuditableEntity
    {
        /// <summary>Identificador de 9 digitos. Se almacena como texto para no perder ceros a la izquierda.</summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>Id del usuario (ASP.NET Identity) propietario de la cuenta.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Balance disponible. Precision decimal(18,2).</summary>
        public decimal Balance { get; set; }

        public AccountType Type { get; set; } = AccountType.Secundaria;

        public AccountStatus Status { get; set; } = AccountStatus.Activa;

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        // Navigation properties
        public ICollection<Transaction>? Transactions { get; set; }
        public ICollection<Beneficiary>? RegisteredAsBeneficiary { get; set; }

        public bool IsActive => Status == AccountStatus.Activa;
        public bool IsPrincipal => Type == AccountType.Principal;

        /// <summary>Ultimos 4 digitos, para correos y comprobantes.</summary>
        public string LastFourDigits =>
            string.IsNullOrEmpty(AccountNumber) || AccountNumber.Length < 4
                ? AccountNumber
                : AccountNumber.Substring(AccountNumber.Length - 4);
    }
}
