namespace ArtemisBank.Core.Application.Dtos.Common
{
    /// <summary>
    /// CONTRATO CONGELADO — proyeccion de solo lectura de una tarjeta de credito.
    /// Nunca expone el numero completo ni el CVC (ni su hash).
    /// Provee: Manuel (modulo de tarjetas). Consume: Michael (cajero, pagos, indicadores).
    /// </summary>
    public class CreditCardInfoDto
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        /// <summary>Solo los ultimos 4 digitos. El numero completo nunca sale del modulo de tarjetas.</summary>
        public string LastFourDigits { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal Debt { get; set; }
        public decimal AvailableCredit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool HasDebt => Debt > 0m;
        public string MaskedNumber => "**** **** **** " + LastFourDigits;
    }
}
