namespace ArtemisBank.Core.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }

        public static NotFoundException For(string entity, object key)
            => new NotFoundException($"No se encontro {entity} con identificador '{key}'.");
    }
}
