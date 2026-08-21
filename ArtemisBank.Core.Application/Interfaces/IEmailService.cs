namespace ArtemisBank.Core.Application.Interfaces
{
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }

    public interface IEmailService
    {
        Task<bool> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
    }
}
