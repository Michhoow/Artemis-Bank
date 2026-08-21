using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    public class CardConsumption : AuditableEntity
    {
        public int CreditCardId { get; set; }

        public decimal Amount { get; set; }

        public ConsumptionType Type { get; set; } = ConsumptionType.Consumo;

        public ConsumptionStatus Status { get; set; } = ConsumptionStatus.Aprobado;

        public string Description { get; set; } = string.Empty;

        public string? CommerceName { get; set; }

        public string? RejectionReason { get; set; }

        public string OperationReference { get; set; } = string.Empty;

        public CreditCard? CreditCard { get; set; }

        public bool IsApproved => Status == ConsumptionStatus.Aprobado;
    }
}
