using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>
    /// El cliente es o se convierte en cliente de alto riesgo y no se confirmo la asignacion.
    /// Se traduce a 409 Conflict en la API, adjuntando el desglose de riesgo en el cuerpo.
    /// </summary>
    public class HighRiskConflictException : Exception
    {
        public HighRiskConflictDto Detail { get; }

        public HighRiskConflictException(HighRiskConflictDto detail) : base(detail.Message)
        {
            Detail = detail;
        }
    }
}
