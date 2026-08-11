namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Estado de la cuenta de ahorro. Nunca se elimina fisicamente: se cancela.</summary>
    public enum AccountStatus
    {
        Activa = 1,
        Cancelada = 2
    }
}
