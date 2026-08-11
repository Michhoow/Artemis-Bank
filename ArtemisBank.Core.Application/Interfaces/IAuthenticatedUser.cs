namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// CONTRATO CONGELADO — usuario autenticado del request actual.
    /// IMPLEMENTA: Monserrat (WebApp con cookies, WebApi con JWT).
    /// CONSUME: Michael para asociar cada operacion al cliente/cajero/administrador responsable.
    /// </summary>
    public interface IAuthenticatedUser
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
