namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    public interface IAccountAuthService
    {
        Task<(string? Token, string? Error)> LoginAsync(string userNameOrEmail, string password);

        Task<(bool Success, string? Error)> ConfirmAccountAsync(string userId, string token);

        Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(string email);

        Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword);
    }
}
