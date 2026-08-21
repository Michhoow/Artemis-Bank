using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Infrastructure.Identity.Interfaces;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    public class WebAppAccountAuthAdapter : IAccountAuthService
    {
        private static readonly string[] AllowedWebAppRoles =
        {
            "Administrador", "Cajero", "Cliente"
        };

        private readonly IAccountServiceForWebApp _accountService;

        public WebAppAccountAuthAdapter(IAccountServiceForWebApp accountService)
            => _accountService = accountService;

        public async Task<(string? Token, string? Error)> LoginAsync(
            string userNameOrEmail, string password)
        {
            var (success, error) = await _accountService.LoginAsync(
                userNameOrEmail, password, AllowedWebAppRoles);

            return success ? (string.Empty, null) : (null, error);
        }

        public Task<(bool Success, string? Error)> ConfirmAccountAsync(string userId, string token)
            => _accountService.ConfirmEmailAsync(userId, token);

        public async Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(string email)
        {
            await _accountService.SendPasswordResetEmailAsync(email);
            return (null, null, null);
        }

        public Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword)
            => _accountService.ResetPasswordAsync(userId, token, newPassword);
    }
}
