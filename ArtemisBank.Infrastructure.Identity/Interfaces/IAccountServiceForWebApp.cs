using ArtemisBank.Infrastructure.Identity.Entities;

namespace ArtemisBank.Infrastructure.Identity.Interfaces
{
    public interface IAccountServiceForWebApp
    {
        Task<(bool Success, string? Error)> LoginAsync(
            string userNameOrEmail, string password, string[] allowedRoles);

        Task LogoutAsync();

        Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName, string lastName, string identification,
            string email, string userName, string password, string role);

        Task<(bool Success, string? Error)> ConfirmEmailAsync(string userId, string token);

        Task SendPasswordResetEmailAsync(string email);

        Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword);
    }
}
