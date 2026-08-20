using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Cuota de la tabla de amortizacion de un prestamo.
    /// Propiedad: Manuel.
    /// El interes y el capital de cada cuota se calculan por el sistema frances al crear el prestamo.
    /// El abono se aplica primero a interes y luego a capital dentro de PendingAmount.
    /// </summary>
    public class Installment : AuditableEntity
    {
        public int LoanId { get; set; }

        /// <summary>Posicion de la cuota dentro del prestamo (1..n).</summary>
        public int InstallmentNumber { get; set; }

        public DateTime DueDate { get; set; }

        /// <summary>Valor total fijo de la cuota (interes + capital).</summary>
        public decimal InstallmentAmount { get; set; }

        /// <summary>Parte de la cuota correspondiente a intereses.</summary>
        public decimal InterestAmount { get; set; }

        /// <summary>Parte de la cuota correspondiente a amortizacion de capital.</summary>
        public decimal CapitalAmount { get; set; }

        /// <summary>Monto pendiente por pagar de esta cuota. Inicia igual a InstallmentAmount.</summary>
        public decimal PendingAmount { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pendiente;

        /// <summary>Indicador de atraso. Lo mantiene al dia el proceso diario de Azure Functions.</summary>
        public bool IsLate { get; set; }

        // Navigation properties
        public Loan? Loan { get; set; }

        public bool IsFullyPaid => Status == InstallmentStatus.Pagada;
    }
}
