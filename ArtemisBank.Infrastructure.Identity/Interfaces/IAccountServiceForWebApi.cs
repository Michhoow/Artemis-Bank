using ArtemisBank.Infrastructure.Identity.Entities;

namespace ArtemisBank.Infrastructure.Identity.Interfaces
{
    /// <summary>
    /// Contrato del servicio de cuenta para la Web API REST.
    /// Los controladores API (AccountController, UsersController) dependen de esta abstracción.
    /// </summary>
    public interface IAccountServiceForWebApi
    {
        /// <summary>
        /// POST /api/account/login — retorna JWT si activo y credenciales válidas.
        /// </summary>
        Task<(string? Token, string? Error)> LoginAsync(
            string userNameOrEmail, string password);

        /// <summary>
        /// POST /api/account/confirm — activa la cuenta (token de un solo uso).
        /// </summary>
        Task<(bool Success, string? Error)> ConfirmAccountAsync(
            string userId, string token);

        /// <summary>
        /// POST /api/account/get-reset-token — genera token (30 min, un solo uso)
        /// y lo envía en el cuerpo del correo, NO como enlace.
        /// </summary>
        Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(string email);

        /// <summary>
        /// POST /api/account/reset-password — aplica el nuevo password y reactiva si inactivo.
        /// </summary>
        Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword);

        /// <summary>
        /// Crea un usuario API (inactivo) y envía correo de activación.
        /// </summary>
        Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName, string lastName, string identification,
            string email, string userName, string password, string role);

        /// <summary>
        /// Activa o desactiva un usuario. Bloquea auto-desactivación.
        /// </summary>
        Task<(bool Success, string? Error)> SetActiveStatusAsync(
            string targetUserId, bool isActive, string? callerUserId);
    }
}
