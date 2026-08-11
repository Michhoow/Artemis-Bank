namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Resultado de la transaccion. Las RECHAZADAS se registran pero no afectan balances.</summary>
    public enum TransactionStatus
    {
        Aprobada = 1,
        Rechazada = 2
    }
}
