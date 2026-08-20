namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Estado del prestamo. Nunca se elimina fisicamente: pasa a Completado cuando se salda.</summary>
    public enum LoanStatus
    {
        Activo = 1,
        Completado = 2
    }
}
