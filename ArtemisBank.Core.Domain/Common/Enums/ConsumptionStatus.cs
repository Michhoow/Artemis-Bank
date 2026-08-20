namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>
    /// Resultado de un consumo o intento de consumo sobre una tarjeta de credito.
    /// Los RECHAZADOS se registran en el historial pero no afectan deuda ni balances.
    /// </summary>
    public enum ConsumptionStatus
    {
        Aprobado = 1,
        Rechazado = 2
    }
}
