namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// CONTRATO CONGELADO — hashing seguro de datos sensibles (CVC de tarjetas).
    /// IMPLEMENTA: ArtemisBank.Infrastructure.Shared (SHA-256).
    /// El CVC en texto plano nunca se almacena ni se retorna: solo se compara su hash.
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>Devuelve el hash SHA-256 (hex) del valor recibido.</summary>
        string Hash(string plainText);

        /// <summary>Compara un valor en claro contra un hash almacenado, en tiempo constante.</summary>
        bool Verify(string plainText, string storedHash);
    }
}
