using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.Transactions
{
    public class CreditCardOptionViewModel
    {
        public int Id { get; set; }
        public string MaskedNumber { get; set; } = string.Empty;
        public decimal Debt { get; set; }
        public string Display => $"{MaskedNumber} - Deuda: RD${Debt:N2}";
    }

    public class LoanOptionViewModel
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal PendingAmount { get; set; }
        public string Display => $"{LoanNumber} - Pendiente: RD${PendingAmount:N2}";
    }

    /// <summary>Pago a tarjeta de credito desde el cliente.</summary>
    public class ClientCardPaymentViewModel
    {
        [Required(ErrorMessage = "La tarjeta de crédito destino es requerida.")]
        [Display(Name = "Tarjeta de crédito destino")]
        public int? CreditCardId { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        [Display(Name = "Cuenta de origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        [Display(Name = "Monto a pagar")]
        public decimal? Amount { get; set; }

        public List<CreditCardOptionViewModel> CreditCards { get; set; } = new();
        public List<AccountOptionViewModel> SourceAccounts { get; set; } = new();
    }

    /// <summary>Pago a prestamo desde el cliente.</summary>
    public class ClientLoanPaymentViewModel
    {
        [Required(ErrorMessage = "El préstamo a pagar es requerido.")]
        [Display(Name = "Préstamo a pagar")]
        public int? LoanId { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        [Display(Name = "Cuenta de origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        [Display(Name = "Monto a pagar")]
        public decimal? Amount { get; set; }

        public List<LoanOptionViewModel> Loans { get; set; } = new();
        public List<AccountOptionViewModel> SourceAccounts { get; set; } = new();
    }

    /// <summary>Transaccion hacia un beneficiario registrado.</summary>
    public class BeneficiaryTransactionViewModel
    {
        [Required(ErrorMessage = "El beneficiario es requerido.")]
        [Display(Name = "Beneficiario")]
        public int? BeneficiaryId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        [Display(Name = "Monto a transferir")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        [Display(Name = "Cuenta de origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        public List<ArtemisBank.Core.Application.ViewModels.Beneficiaries.BeneficiaryViewModel> Beneficiaries { get; set; } = new();
        public List<AccountOptionViewModel> SourceAccounts { get; set; } = new();
    }
}
