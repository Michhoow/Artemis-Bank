using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Identity.Entities;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    /// <summary>
    /// Servicio de cuenta para la Web API REST.
    /// Endpoints públicos (sin JWT previo): login, confirm, get-reset-token, reset-password.
    /// Reglas de negocio (DoD MON-03):
    ///   - Login retorna JWT solo si <see cref="AppUser.IsActive"/> = true.
    ///   - Confirm activa la cuenta y marca el token como usado (un solo uso, Identity nativo).
    ///   - Reset: token vigente 30 min, de un solo uso; el correo lleva el token en el cuerpo,
    ///     NO como enlace.
    ///   - Respuestas HTTP: 200, 204, 400, 401, 403, 409.
    /// </summary>
    public class AccountServiceForWebApi : IAccountServiceForWebApi
    {
        private readonly UserManager<AppUser>         _userManager;
        private readonly JwtService                   _jwtService;
        private readonly IEmailService                _emailService;
        private readonly IAccountNumberGenerator     _accountNumberGenerator;
        private readonly ISavingsAccountRepository _savingsAccountRepository;
        private readonly ICommerceRepository          _commerceRepository;
        private readonly ILogger<AccountServiceForWebApi> _logger;

        public AccountServiceForWebApi(
            UserManager<AppUser>              userManager,
            JwtService                        jwtService,
            IEmailService                     emailService,
            IAccountNumberGenerator           accountNumberGenerator,
            ISavingsAccountRepository         savingsAccountRepository,
            ICommerceRepository              commerceRepository,
            ILogger<AccountServiceForWebApi>  logger)
        {
            _userManager              = userManager;
            _jwtService               = jwtService;
            _emailService             = emailService;
            _accountNumberGenerator   = accountNumberGenerator;
            _savingsAccountRepository = savingsAccountRepository;
            _commerceRepository       = commerceRepository;
            _logger                   = logger;
        }

        // ── POST /api/account/login ──────────────────────────────────────────

        /// <summary>
        /// Autentica con username/email + password.
        /// Devuelve el JWT solo si la cuenta está activa.
        /// </summary>
        /// <returns>
        /// (Token, null) en éxito · (null, error) en fallo.
        /// El controlador decide el código HTTP basándose en el error.
        /// </returns>
        public async Task<(string? Token, string? Error)> LoginAsync(
            string userNameOrEmail, string password)
        {
            var user = await _userManager.FindByNameAsync(userNameOrEmail)
                    ?? await _userManager.FindByEmailAsync(userNameOrEmail);

            if (user is null)
            {
                _logger.LogWarning("API Login: usuario '{User}' no encontrado.", userNameOrEmail);
                return (null, "Credenciales inválidas.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("API Login: usuario '{User}' inactivo.", userNameOrEmail);
                return (null, "Su cuenta no está activa.");
            }

            var passwordOk = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordOk)
            {
                _logger.LogWarning("API Login: contraseña incorrecta para '{User}'.", userNameOrEmail);
                return (null, "Credenciales inválidas.");
            }

            var token = await _jwtService.GenerateTokenAsync(user);
            _logger.LogInformation("API Login exitoso: '{User}'.", userNameOrEmail);
            return (token, null);
        }

        // ── POST /api/account/confirm ────────────────────────────────────────

        /// <summary>
        /// Confirma el correo del usuario y activa su cuenta.
        /// Token de un solo uso (ASP.NET Identity lo invalida tras el primer uso).
        /// </summary>
        public async Task<(bool Success, string? Error)> ConfirmAccountAsync(
            string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return (false, "Usuario no encontrado.");

            if (user.EmailConfirmed && user.IsActive)
                return (false, "La cuenta ya estaba activada.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            user.IsActive = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return (false, JoinErrors(updateResult));

            // Si es un comercio, activar también la entidad Commerce
            var commerce = await _commerceRepository.GetByUserIdAsync(user.Id);
            if (commerce is not null)
            {
                commerce.IsActive = true;
                await _commerceRepository.UpdateAsync(commerce.Id, commerce);
            }

            _logger.LogInformation("API Confirm: cuenta activada para userId '{UserId}'.", userId);
            return (true, null);
        }

        // ── POST /api/account/get-reset-token ────────────────────────────────

        /// <summary>
        /// Genera un token de reset de contraseña válido 30 min y lo devuelve en el cuerpo
        /// de la respuesta JSON (NO como enlace). Comportamiento silencioso si el correo
        /// no existe para no revelar registros.
        /// </summary>
        public async Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(
            string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                // Respuesta silenciosa — seguridad: no revelar si el correo está registrado
                _logger.LogInformation("API GetResetToken: email '{Email}' no encontrado (silencioso).", email);
                return (null, null, null);  // El controlador responde 204
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // El correo lleva el token en el cuerpo (no como enlace), según DoD MON-03
            await SendResetTokenEmailAsync(user, token);

            _logger.LogInformation("API GetResetToken: token enviado a '{Email}'.", email);
            return (token, user.Id, null);
        }

        // ── POST /api/account/reset-password ────────────────────────────────

        /// <summary>
        /// Aplica la nueva contraseña usando el token de un solo uso.
        /// Si la cuenta estaba inactiva, la reactiva.
        /// </summary>
        public async Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId,
            string token,
            string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return (false, "Usuario no encontrado.");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            // Reactivar la cuenta si estaba inactiva (flujo de desbloqueo)
            if (!user.IsActive)
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("API ResetPassword: cuenta reactivada para '{UserId}'.", userId);
            }

            _logger.LogInformation("API ResetPassword: contraseña restablecida para '{UserId}'.", userId);
            return (true, null);
        }

        // ── Registro de usuario vía API (para Administrador) ─────────────────

        /// <summary>
        /// Crea un usuario API (rol Cliente o Comercio) con estado inactivo.
        /// RNC y correo deben ser únicos (validado aquí y en el nivel de BD).
        /// </summary>
        public async Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName,
            string lastName,
            string identification,
            string email,
            string userName,
            string password,
            string role)
        {
            // Unicidad de cédula/RNC
            var identExisting = _userManager.Users
                .Any(u => u.Identification == identification);
            if (identExisting)
                return (false, "Ya existe un usuario con esa identificación/RNC.", null);

            // Unicidad de correo (UserManager ya lo valida, pero mensaje más claro)
            var emailExisting = await _userManager.FindByEmailAsync(email);
            if (emailExisting is not null)
                return (false, "El correo electrónico ya está registrado.", null);

            var user = new AppUser
            {
                FirstName      = firstName,
                LastName       = lastName,
                Identification = identification,
                Email          = email,
                UserName       = userName,
                IsActive       = false,       // Nace inactivo
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return (false, JoinErrors(createResult), null);

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user); // rollback parcial
                return (false, $"No se pudo asignar el rol '{role}': {JoinErrors(roleResult)}", null);
            }

            // Si el rol es Cliente o Comercio, generar la cuenta principal de ahorro
            if (string.Equals(role, DefaultRoles.Cliente, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, DefaultRoles.Comercio, StringComparison.OrdinalIgnoreCase))
            {
                var accountNumber = await _accountNumberGenerator.GenerateAsync();
                await _savingsAccountRepository.AddAsync(new SavingsAccount
                {
                    AccountNumber   = accountNumber,
                    ClientId        = user.Id,
                    Balance         = 0.00m,
                    Type            = AccountType.Principal,
                    Status          = AccountStatus.Activa,
                    CreatedAt       = DateTime.Now,
                    CreatedByUserId = user.Id
                });

                // Si es Comercio, registrar la entidad Comercio
                if (string.Equals(role, DefaultRoles.Comercio, StringComparison.OrdinalIgnoreCase))
                {
                    await _commerceRepository.AddAsync(new Commerce
                    {
                        Name          = $"{user.FirstName} {user.LastName}".Trim(),
                        Rnc           = user.Identification,
                        Email         = user.Email ?? string.Empty,
                        Phone         = user.PhoneNumber ?? string.Empty,
                        UserId        = user.Id,
                        AccountNumber = accountNumber,
                        IsActive      = false, // Inactivo hasta que confirme cuenta
                        CreatedAt     = DateTime.Now,
                        CreatedByUserId = user.Id
                    });
                }
            }

            // Enviar token de activación en el correo
            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendActivationEmailAsync(user, confirmToken);

            _logger.LogInformation(
                "API Register: usuario '{UserName}' creado con rol '{Role}', inactivo.", userName, role);

            return (true, null, user);
        }

        // ── Activar / Desactivar usuario ─────────────────────────────────────

        /// <summary>
        /// Cambia el estado activo de un usuario.
        /// Un administrador no puede desactivarse a sí mismo (regla MON-04).
        /// </summary>
        public async Task<(bool Success, string? Error)> SetActiveStatusAsync(
            string targetUserId, bool isActive, string? callerUserId)
        {
            if (targetUserId == callerUserId && !isActive)
                return (false, "No puede desactivar su propia cuenta.");

            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user is null)
                return (false, "Usuario no encontrado.");

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            _logger.LogInformation(
                "Estado del usuario '{UserId}' cambiado a IsActive={IsActive} por '{Caller}'.",
                targetUserId, isActive, callerUserId);

            return (true, null);
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private async Task SendActivationEmailAsync(AppUser user, string token)
        {
            // El token va en el cuerpo, no como enlace, para que el consumidor
            // haga POST /api/account/confirm con el token.
            var body = $"""
                <h2>Artemis Banking Pro — Activación de cuenta</h2>
                <p>Hola {user.FirstName},</p>
                <p>Su cuenta ha sido creada. Use los siguientes datos para activarla:</p>
                <ul>
                  <li><strong>UserId:</strong> {user.Id}</li>
                  <li><strong>Token:</strong> {token}</li>
                </ul>
                <p>Envíe un POST a <code>/api/account/confirm</code> con estos valores.</p>
                <p>Este token es de un solo uso.</p>
                """;

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Activar cuenta API",
                HtmlBody = body
            });
        }

        private async Task SendResetTokenEmailAsync(AppUser user, string token)
        {
            // Según DoD MON-03: el token va en el cuerpo del correo, NO como enlace.
            var body = $"""
                <h2>Artemis Banking Pro — Restablecimiento de contraseña</h2>
                <p>Hola {user.FirstName},</p>
                <p>Se solicitó un restablecimiento de contraseña para su cuenta.</p>
                <ul>
                  <li><strong>UserId:</strong> {user.Id}</li>
                  <li><strong>Token:</strong> {token}</li>
                </ul>
                <p>Envíe un POST a <code>/api/account/reset-password</code> con estos valores.</p>
                <p>El token expira en <strong>30 minutos</strong> y es de un solo uso.</p>
                <p>Si no lo solicitó, ignore este correo.</p>
                """;

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Restablecer contraseña API",
                HtmlBody = body
            });
        }

        private static string JoinErrors(IdentityResult result)
            => string.Join("; ", result.Errors.Select(e => e.Description));
    }
}
