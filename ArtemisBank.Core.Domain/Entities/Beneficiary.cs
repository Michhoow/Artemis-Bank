using ArtemisBank.Core.Domain.Common;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Relacion entre un cliente y una cuenta de ahorro de OTRO cliente registrada como beneficiaria.
    /// Eliminar un beneficiario solo elimina la relacion; nunca la cuenta ni el historial.
    /// </summary>
    public class Beneficiary : AuditableEntity
    {
        /// <summary>Cliente propietario del registro de beneficiario.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Cuenta de ahorro registrada como beneficiaria.</summary>
        public int SavingsAccountId { get; set; }

        // Navigation properties
        public SavingsAccount? SavingsAccount { get; set; }
    }
}
