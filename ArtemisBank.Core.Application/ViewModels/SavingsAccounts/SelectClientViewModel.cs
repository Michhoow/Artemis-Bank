using System.ComponentModel.DataAnnotations;

namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    public class ClientRowViewModel
    {
        public string ClientId { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public bool HasActivePrincipalAccount { get; set; }
    }

    /// <summary>Paso 1 de la asignacion: seleccion del cliente mediante radio button.</summary>
    public class SelectClientViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public string? ClientId { get; set; }

        public string? Search { get; set; }

        public List<ClientRowViewModel> Clients { get; set; } = new();
    }
}
