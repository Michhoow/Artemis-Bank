namespace ArtemisBank.Core.Domain.Settings
{
    public class MailSettings
    {
        public string EmailFrom { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "Artemis Banking Pro";

        public bool Disabled { get; set; }
    }
}
