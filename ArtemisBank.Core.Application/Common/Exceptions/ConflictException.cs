namespace ArtemisBank.Core.Application.Common.Exceptions
{
    /// <summary>Conflicto de estado. Se traduce a 409 Conflict.</summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
