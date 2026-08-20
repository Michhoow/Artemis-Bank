using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Tarjeta de credito. Maneja los datos mas sensibles del sistema.
    /// Propiedad: Manuel.
    /// El numero completo (16 digitos) y el CVC NUNCA salen del modulo de tarjetas:
    /// el CVC se guarda como hash SHA-256 y el numero solo se expone por sus ultimos 4 digitos.
    /// </summary>
    public class CreditCard : AuditableEntity
    {
        /// <summary>Numero de 16 digitos. Texto para no perder ceros a la izquierda. Nunca se expone completo.</summary>
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>Id del usuario (ASP.NET Identity) duenio de la tarjeta.</summary>
        public string ClientId { get; set; } = string.Empty;

        public decimal CreditLimit { get; set; }

        /// <summary>Deuda acumulada. Inicia en 0 y solo sube con consumos o avances APROBADOS.</summary>
        public decimal Debt { get; set; }

        /// <summary>Hash SHA-256 del CVC de 3 digitos. Nunca se almacena ni retorna en texto plano.</summary>
        public string CvcHash { get; set; } = string.Empty;

        /// <summary>Expiracion en formato MM/AA. Se calcula sumando 3 anios a la fecha de creacion.</summary>
        public string ExpirationDate { get; set; } = string.Empty;

        public CreditCardStatus Status { get; set; } = CreditCardStatus.Activa;

        /// <summary>Administrador que asigno la tarjeta.</summary>
        public string? AssignedByUserId { get; set; }

        // Navigation properties
        public ICollection<Consumption> Consumptions { get; set; } = new List<Consumption>();

        public bool IsActive => Status == CreditCardStatus.Activa;

        public bool HasDebt => Debt > 0m;

        public decimal AvailableCredit => CreditLimit - Debt;

        /// <summary>Ultimos 4 digitos, unica parte del numero que puede mostrarse.</summary>
        public string LastFourDigits =>
            string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 4
                ? CardNumber
                : CardNumber.Substring(CardNumber.Length - 4);
    }
}
