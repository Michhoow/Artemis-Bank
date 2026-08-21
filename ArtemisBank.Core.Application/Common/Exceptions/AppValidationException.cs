namespace ArtemisBank.Core.Application.Common.Exceptions
{
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
