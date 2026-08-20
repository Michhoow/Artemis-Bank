using System.Security.Cryptography;
using System.Text;
using ArtemisBank.Core.Application.Interfaces;

namespace ArtemisBank.Infrastructure.Shared.Services
{
    /// <summary>
    /// Hashing SHA-256 para el CVC de las tarjetas de credito.
    /// El requerimiento tecnico exige SHA-256 o un mecanismo equivalente; el CVC nunca se
    /// guarda ni se compara en texto plano.
    /// </summary>
    public class CryptoService : ICryptoService
    {
        public string Hash(string plainText)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToHexString(bytes); // mayusculas, sin separadores
        }

        public bool Verify(string plainText, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            var computed = Hash(plainText ?? string.Empty);

            // Comparacion en tiempo constante para no filtrar informacion por temporizacion.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(computed),
                Encoding.ASCII.GetBytes(storedHash));
        }
    }
}
