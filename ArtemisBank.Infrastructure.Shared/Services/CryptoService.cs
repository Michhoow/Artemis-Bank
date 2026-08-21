using System.Security.Cryptography;
using System.Text;
using ArtemisBank.Core.Application.Interfaces;

namespace ArtemisBank.Infrastructure.Shared.Services
{
    public class CryptoService : ICryptoService
    {
        private const int SaltSizeInBytes = 32;

        public string GenerateSalt()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeInBytes));

        public string Hash(string value, string salt)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var bytes = Encoding.UTF8.GetBytes(salt + "|" + value);
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }

        public bool Verify(string value, string salt, string expectedHash)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(expectedHash)) return false;

            var computed = Hash(value, salt ?? string.Empty);

            var a = Encoding.UTF8.GetBytes(computed);
            var b = Encoding.UTF8.GetBytes(expectedHash);
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }

        public int NextDigit() => RandomNumberGenerator.GetInt32(0, 10);

        public string RandomDigits(int length)
        {
            if (length <= 0) return string.Empty;

            var builder = new StringBuilder(length);
            for (var i = 0; i < length; i++) builder.Append(NextDigit());
            return builder.ToString();
        }
    }
}
