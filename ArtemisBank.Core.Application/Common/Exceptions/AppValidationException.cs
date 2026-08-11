namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>
    /// Errores estructurales detectados por FluentValidation en el ValidationBehavior.
    /// Se traduce a 400 Bad Request con Problem Details + diccionario de errores.
    /// </summary>
    public class AppValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public AppValidationException(IDictionary<string, string[]> errors)
            : base("Se encontraron uno o mas errores de validacion.")
        {
            Errors = errors;
        }
    }
}
