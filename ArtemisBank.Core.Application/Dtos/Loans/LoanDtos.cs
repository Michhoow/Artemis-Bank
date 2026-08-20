namespace ArtemisBank.Core.Application.Dtos.Loans
{
    /// <summary>Fila del listado de prestamos (WebApp/API). Coincide con el contrato GET /api/loan.</summary>
    public class LoanListItemDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        /// <summary>"Activo" | "Completado".</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>"Al día" | "En mora".</summary>
        public string ClientPaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Linea de la tabla de amortizacion tal como se expone en el detalle.</summary>
    public class InstallmentDto
    {
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal PendingInstallmentAmount { get; set; }
        /// <summary>"Pendiente" | "Parcialmente pagada" | "Pagada".</summary>
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsLate { get; set; }
    }

    /// <summary>Detalle de un prestamo con su tabla de amortizacion (GET /api/loan/{id}).</summary>
    public class LoanDetailDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ClientPaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<InstallmentDto> Amortization { get; set; } = new();
    }

    /// <summary>Respuesta de creacion de prestamo (POST /api/loan → 201).</summary>
    public class LoanCreatedDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal TotalAmountToPay { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Cuerpo del 409 Conflict de alto riesgo.</summary>
    public class HighRiskConflictDto
    {
        public string Message { get; set; } = string.Empty;
        /// <summary>"CurrentHighRisk" | "ProjectedHighRisk".</summary>
        public string RiskType { get; set; } = string.Empty;
        public decimal CurrentDebt { get; set; }
        public decimal ProjectedDebt { get; set; }
        public decimal AverageDebt { get; set; }
    }

    /// <summary>Datos de asignacion de un prestamo (usado por command y servicio).</summary>
    public class AssignLoanDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CapitalAmount { get; set; }
        public int TermInMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public bool ConfirmHighRisk { get; set; }
        public string? AssignedByUserId { get; set; }
    }

    /// <summary>Filtro del listado de prestamos.</summary>
    public class LoanFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>activos | completados | todos.</summary>
        public string? Status { get; set; }
        public string? Identification { get; set; }
    }
}
