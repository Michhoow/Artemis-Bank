namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>Usuario autenticado sin permiso sobre el recurso. Se traduce a 403 Forbidden.</summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
