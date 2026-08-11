namespace ArtemisBank.Core.Domain.Settings
{
    /// <summary>Configuracion del JWT de la Web API. Los valores viven en appsettings / user-secrets.</summary>
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public double DurationInMinutes { get; set; } = 60;
    }
}
