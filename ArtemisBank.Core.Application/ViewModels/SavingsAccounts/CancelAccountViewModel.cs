namespace ArtemisBank.Core.Application.ViewModels.SavingsAccounts
{
    public class CancelAccountViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string ConfirmationMessage => $"¿Está seguro que desea cancelar la cuenta {AccountNumber}?";
    }
}
