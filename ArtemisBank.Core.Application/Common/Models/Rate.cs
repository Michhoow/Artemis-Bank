using System.Globalization;

namespace ArtemisBank.Core.Application.Common.Models
{
    public static class Rate
    {
        private const string TrimFormat = "0.####";

        public static string ForInput(decimal rate)
            => rate.ToString(TrimFormat, CultureInfo.InvariantCulture);

        public static string Format(decimal rate)
            => ForInput(rate) + " %";

        public static decimal Trim(decimal rate)
            => decimal.Parse(ForInput(rate), NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}
