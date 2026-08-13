using ArtemisBank.Infrastructure.Identity.Entities;

namespace ArtemisBank.Infrastructure.Identity.Interfaces
{
    /// <summary>
    /// Contrato del servicio de cuenta para la WebApp MVC.
    /// Los controladores (AccountController, UserController) dependen de esta abstracción.
    /// </summary>
    public interface IAccountServiceForWebApp
    {
        /// <summary>
        /// Autentica al usuario y emite cookie de sesión.
        /// </summary>
        Task<(bool Success, string? Error)> LoginAsync(
            string userNameOrEmail, string password, string[] allowedRoles);

        /// <summary>Invalida la cookie de sesión actual.</summary>
        Task LogoutAsync();

        /// <summary>
        /// Crea un usuario inactivo y envía correo de activación.
        /// </summary>
        Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName, string lastName, string identification,
            string email, string userName, string password, string role);

        /// <summary>
        /// Confirma el correo y activa la cuenta (token de un solo uso).
        /// </summary>
        Task<(bool Success, string? Error)> ConfirmEmailAsync(string userId, string token);

        /// <summary>
        /// Genera token de reset y lo envía por correo.
        /// </summary>
        Task SendPasswordResetEmailAsync(string email);

        /// <summary>
        /// Aplica la nueva contraseña (token de un solo uso, 30 min).
        /// </summary>
        Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword);
    }
}
