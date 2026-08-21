using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Infrastructure.Identity.Entities;

namespace ArtemisBank.Infrastructure.Identity.Interfaces
{
    public interface IAccountServiceForWebApi : IAccountAuthService
    {
        Task<(string? Token, string? Error)> LoginAsync(
            string userNameOrEmail, string password);

        Task<(bool Success, string? Error)> ConfirmAccountAsync(
            string userId, string token);

        Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(string email);

        Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword);

        Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName, string lastName, string identification,
            string email, string userName, string password, string role);

        Task<(bool Success, string? Error)> SetActiveStatusAsync(
            string targetUserId, bool isActive, string? callerUserId);
    }
}
