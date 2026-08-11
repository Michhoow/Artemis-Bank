namespace ArtemisBank.Core.Domain.Common.Enums
{
    /// <summary>Roles del sistema. WebApp: Administrador, Cajero, Cliente. WebApi: Administrador, Comercio.</summary>
    public enum Roles
    {
        Administrador = 1,
        Cajero = 2,
        Cliente = 3,
        Comercio = 4
    }
}
