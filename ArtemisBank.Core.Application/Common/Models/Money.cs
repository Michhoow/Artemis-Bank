namespace ArtemisBank.Core.Application.Common.Models
{
    public static class Money
    {
        public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static string Format(decimal value) => "RD$" + Round(value).ToString("N2");

        public static string LastFour(string? number)
        {
            if (string.IsNullOrWhiteSpace(number)) return string.Empty;
            return number.Length <= 4 ? number : number.Substring(number.Length - 4);
        }

        public static string MaskCard(string? cardNumber)
        {
            var last = LastFour(cardNumber);
            return string.IsNullOrEmpty(last) ? string.Empty : "**** **** **** " + last;
        }
    }
}
