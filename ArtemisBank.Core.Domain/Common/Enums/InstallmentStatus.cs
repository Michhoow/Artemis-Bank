namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Estado de pago de una cuota de la tabla de amortizacion.</summary>
    public enum InstallmentStatus
    {
        Pendiente = 1,
        ParcialmentePagada = 2,
        Pagada = 3
    }
}
