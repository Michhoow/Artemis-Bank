namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    public class SavingsAccountViewModel
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsPrincipal { get; set; }
        public bool IsActive { get; set; }
        /// <summary>La accion Cancelar solo se muestra en cuentas secundarias activas.</summary>
        public bool CanBeCancelled => IsActive && !IsPrincipal;
    }
}
