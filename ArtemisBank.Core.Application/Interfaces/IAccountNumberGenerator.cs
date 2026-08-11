namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Genera identificadores de 9 digitos unicos en TODO el sistema:
    /// no pueden repetirse ni como cuenta de ahorro ni como numero de prestamo.
    /// </summary>
    public interface IAccountNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
