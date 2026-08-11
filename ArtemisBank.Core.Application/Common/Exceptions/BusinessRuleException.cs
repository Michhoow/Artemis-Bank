namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>Regla de negocio incumplida. Se traduce a 400 Bad Request / mensaje en la vista.</summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}
