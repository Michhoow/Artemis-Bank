namespace ArtemisBank.WebApp.Common
{
    /// <summary>Nombres de rol usados en los atributos [Authorize]. Deben coincidir con el seeding de Identity.</summary>
    public static class ArtemisRoles
    {
        public const string Administrador = "Administrador";
        public const string Cajero = "Cajero";
        public const string Cliente = "Cliente";
    }
}
