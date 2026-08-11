namespace ArtemisBank.Core.Domain.Settings
{
    /// <summary>Configuracion SMTP. Usuario y contrasenia viven en user-secrets, nunca en el repo.</summary>
    public class MailSettings
    {
        public string EmailFrom { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "Artemis Banking Pro";
        /// <summary>Si es true no se envia nada: solo se traza. Util en desarrollo y pruebas.</summary>
        public bool Disabled { get; set; }
    }
}
