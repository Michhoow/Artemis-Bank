using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    /// <summary>Paso 2 de la asignacion: configuracion de la cuenta secundaria.</summary>
    public class AssignSavingsAccountViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public string ClientId { get; set; } = string.Empty;

        public string ClientFullName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;

        [Required(ErrorMessage = "El balance inicial es requerido.")]
        [Range(0, 999999999999999.99, ErrorMessage = "El balance inicial no puede ser negativo.")]
        [Display(Name = "Balance inicial")]
        public decimal? InitialBalance { get; set; }
    }
}
