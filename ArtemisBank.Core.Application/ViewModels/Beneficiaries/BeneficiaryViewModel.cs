using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.Beneficiaries
{
    public class BeneficiaryViewModel
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => (FirstName + " " + LastName).Trim();
    }

    public class SaveBeneficiaryViewModel
    {
        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [Display(Name = "Número de cuenta")]
        public string AccountNumber { get; set; } = string.Empty;
    }
}
