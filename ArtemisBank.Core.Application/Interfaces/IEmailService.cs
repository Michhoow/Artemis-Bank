namespace ArtemisBank.Core.Application.Interfaces
{
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }

    /// <summary>
    /// CONTRATO CONGELADO — envio de correo desacoplado y reutilizable.
    /// IMPLEMENTA: ArtemisBank.Infrastructure.Shared.
    /// Un fallo de correo NUNCA revierte una operacion financiera ya confirmada.
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
    }
}
