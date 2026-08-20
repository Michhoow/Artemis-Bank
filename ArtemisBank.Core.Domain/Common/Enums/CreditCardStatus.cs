namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Estado de la tarjeta de credito. Nunca se elimina fisicamente: se cancela.</summary>
    public enum CreditCardStatus
    {
        Activa = 1,
        Cancelada = 2
    }
}
