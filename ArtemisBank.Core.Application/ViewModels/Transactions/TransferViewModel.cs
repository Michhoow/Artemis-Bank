using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.Transactions
{
    public class TransferViewModel
    {
        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        [Display(Name = "Cuenta de origen")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cuenta de destino es requerida.")]
        [Display(Name = "Cuenta de destino")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, 999999999999999.99, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        [Display(Name = "Monto a transferir")]
        public decimal? Amount { get; set; }

        public List<AccountOptionViewModel> Accounts { get; set; } = new();
    }

    public class ConfirmTransferViewModel
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string TargetAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
