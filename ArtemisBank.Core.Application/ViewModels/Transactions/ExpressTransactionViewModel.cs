using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.Transactions
{
    public class AccountOptionViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Display => $"{AccountNumber} ({Type}) - RD${Balance:N2}";
    }

    public class ExpressTransactionViewModel
    {
        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta destino")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        [Display(Name = "Monto a transferir")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        [Display(Name = "Cuenta de origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        public List<AccountOptionViewModel> SourceAccounts { get; set; } = new();
    }

    /// <summary>Pantalla de confirmacion previa a ejecutar la transaccion.</summary>
    public class ConfirmTransactionViewModel
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public string TargetOwnerFullName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>Express o Beneficiario. Define el endpoint de confirmacion.</summary>
        public string Mode { get; set; } = "Express";
        public int? BeneficiaryId { get; set; }
    }
}
