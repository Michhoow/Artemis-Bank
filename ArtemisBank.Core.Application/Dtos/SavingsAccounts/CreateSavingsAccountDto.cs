namespace ArtemisBank.Core.Application.Dtos.SavingsAccounts
{
    /// <summary>Body de POST /api/savings-account. Solo crea cuentas secundarias.</summary>
    public class CreateSavingsAccountDto
    {
        /// <summary>Identificador del cliente al que se asignara la cuenta secundaria.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Balance inicial. Puede ser 0.00 pero no puede ser negativo.</summary>
        public decimal InitialBalance { get; set; }
    }
}
