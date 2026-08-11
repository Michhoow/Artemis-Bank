namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>Recurso inexistente. Se traduce a 404 Not Found.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }

        public static NotFoundException For(string entity, object key)
            => new NotFoundException($"No se encontro {entity} con identificador '{key}'.");
    }
}
