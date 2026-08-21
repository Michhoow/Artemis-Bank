using System.Text.RegularExpressions;

namespace ArtemisBank.Core.Application.Common.Models
{
    public static class Cedula
    {
        public const int Digits = 11;

        public const int FormattedLength = Digits + 2;

        public static string Normalize(string? value)
            => string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, @"\D", string.Empty);

        public static bool IsValid(string? value)
        {
            var digits = Normalize(value);
            return digits.Length == Digits && digits.All(char.IsDigit);
        }

        public static string Format(string? value)
        {
            var digits = Normalize(value);
            if (digits.Length != Digits) return value ?? string.Empty;

            return $"{digits[..3]}-{digits[3..10]}-{digits[10..]}";
        }
    }
}
