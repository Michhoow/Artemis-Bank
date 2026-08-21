namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    public class CreateSavingsAccountDto
    {
        public string ClientId { get; set; } = string.Empty;

        public decimal InitialBalance { get; set; }
    }
}
