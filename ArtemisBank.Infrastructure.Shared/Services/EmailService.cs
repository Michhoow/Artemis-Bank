using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ArtemisBank.Infrastructure.Shared.Services
{
    /// <summary>
    /// Servicio de correo desacoplado y reutilizable.
    /// Devuelve false ante cualquier fallo en lugar de propagar la excepcion: las operaciones
    /// financieras ya confirmadas NUNCA se revierten por un problema de notificacion.
    /// Nunca registra en log el cuerpo del mensaje ni credenciales SMTP.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly MailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.To)) return false;

            if (_settings.Disabled)
            {
                _logger.LogInformation("Envio de correo deshabilitado. Asunto: {Subject}", request.Subject);
                return true;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.EmailFrom));
                message.To.Add(MailboxAddress.Parse(request.To));
                message.Subject = request.Subject;
                message.Body = new BodyBuilder { HtmlBody = request.HtmlBody }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort,
                    SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Correo enviado correctamente. Asunto: {Subject}", request.Subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar el correo. Asunto: {Subject}", request.Subject);
                return false;
            }
        }
    }
}
