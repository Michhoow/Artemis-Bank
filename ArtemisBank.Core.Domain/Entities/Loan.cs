using ArtemisBank.Core.Domain.Common;
using ArtemisBank.Core.Domain.Common.Enums;

namespace ArtemisBank.Core.Domain.Entities
{
    /// <summary>
    /// Prestamo bajo sistema frances de amortizacion (cuota fija).
    /// Propiedad: Manuel.
    /// Un cliente solo puede tener un prestamo Activo a la vez.
    /// El numero de prestamo comparte espacio con los numeros de cuenta: 9 digitos unicos en TODO el sistema.
    /// </summary>
    public class Loan : AuditableEntity
    {
        /// <summary>Identificador de 9 digitos. Se almacena como texto para no perder ceros a la izquierda.</summary>
        public string LoanNumber { get; set; } = string.Empty;

        /// <summary>Id del usuario (ASP.NET Identity) duenio del prestamo.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Capital aprobado y desembolsado. Precision decimal(18,2).</summary>
        public decimal ApprovedCapital { get; set; }

        /// <summary>Tasa de interes ANUAL vigente. Se puede editar y recalcula solo cuotas futuras.</summary>
        public decimal AnnualInterestRate { get; set; }

        /// <summary>Plazo en meses. Multiplo de 6 entre 6 y 60.</summary>
        public int TermInMonths { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Activo;

        /// <summary>Administrador que aprobo el prestamo.</summary>
        public string? AssignedByUserId { get; set; }

        // Navigation properties
        public ICollection<Installment> Installments { get; set; } = new List<Installment>();

        public bool IsActive => Status == LoanStatus.Activo;

        /// <summary>Suma del pendiente de todas las cuotas.</summary>
        public decimal PendingAmount =>
            Installments == null ? 0m : Installments.Sum(i => i.PendingAmount);

        public int TotalInstallments => Installments?.Count ?? 0;

        public int PaidInstallments =>
            Installments == null ? 0 : Installments.Count(i => i.Status == InstallmentStatus.Pagada);

        /// <summary>En mora si tiene al menos una cuota vencida y no pagada por completo.</summary>
        public bool IsOverdue =>
            Installments != null && Installments.Any(i => i.IsLate && i.Status != InstallmentStatus.Pagada);
    }
}
