namespace ArtemisBank.Core.Application.Dtos.Loans
{
    /// <summary>Linea calculada de la tabla de amortizacion (sistema frances). No se persiste tal cual.</summary>
    public class AmortizationLineDto
    {
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal CapitalAmount { get; set; }
    }
}
