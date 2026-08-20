using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Consumo (o intento de consumo) sobre una tarjeta de credito.
    /// Propiedad: Manuel.
    /// Cubre pagos a comercio via Hermes Pay y avances de efectivo (CommerceName = "AVANCE").
    /// Los intentos RECHAZADOS se guardan pero no modifican deuda ni balances.
    /// </summary>
    public class Consumption : AuditableEntity
    {
        public int CreditCardId { get; set; }

        /// <summary>Monto total cargado a la tarjeta. En avances incluye el interes del 6.25%.</summary>
        public decimal Amount { get; set; }

        /// <summary>Nombre del comercio. Para avances de efectivo se guarda el texto "AVANCE".</summary>
        public string CommerceName { get; set; } = string.Empty;

        /// <summary>Comercio receptor cuando el consumo proviene de Hermes Pay. Null en avances.</summary>
        public int? CommerceId { get; set; }

        public ConsumptionStatus Status { get; set; } = ConsumptionStatus.Aprobado;

        /// <summary>Motivo cuando Status = Rechazado. Nunca contiene datos sensibles.</summary>
        public string? RejectionReason { get; set; }

        // Navigation properties
        public CreditCard? CreditCard { get; set; }

        public bool IsAdvance => string.Equals(CommerceName, "AVANCE", StringComparison.OrdinalIgnoreCase);
    }
}
