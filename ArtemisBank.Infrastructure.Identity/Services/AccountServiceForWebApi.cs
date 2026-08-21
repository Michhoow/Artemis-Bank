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

            var commerce = await _commerceRepository.GetByUserIdAsync(user.Id);
            if (commerce is not null)
            {
                commerce.IsActive = true;
                await _commerceRepository.UpdateEntityAsync(commerce);
            }

            _logger.LogInformation("API Confirm: cuenta activada para userId '{UserId}'.", userId);
            return (true, null);
        }

        public async Task<(string? Token, string? UserId, string? Error)> GetResetTokenAsync(
            string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                _logger.LogInformation("API GetResetToken: email '{Email}' no encontrado (silencioso).", email);
                return (null, null, null);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            await SendResetTokenEmailAsync(user, token);

            _logger.LogInformation("API GetResetToken: token enviado a '{Email}'.", email);
            return (token, user.Id, null);
        }

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

            if (!user.IsActive)
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("API ResetPassword: cuenta reactivada para '{UserId}'.", userId);
            }

            _logger.LogInformation("API ResetPassword: contraseña restablecida para '{UserId}'.", userId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(
            string firstName,
            string lastName,
            string identification,
            string email,
            string userName,
            string password,
            string role)
        {
            var identExisting = _userManager.Users
                .Any(u => u.Identification == identification);
            if (identExisting)
                return (false, "Ya existe un usuario con esa identificación/RNC.", null);

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
                IsActive       = false,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return (false, JoinErrors(createResult), null);

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, $"No se pudo asignar el rol '{role}': {JoinErrors(roleResult)}", null);
            }

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
                        IsActive      = false,
                        CreatedAt     = DateTime.Now,
                        CreatedByUserId = user.Id
                    });
                }
            }

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendActivationEmailAsync(user, confirmToken);

            _logger.LogInformation(
                "API Register: usuario '{UserName}' creado con rol '{Role}', inactivo.", userName, role);

            return (true, null, user);
        }

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

        private async Task SendActivationEmailAsync(AppUser user, string token)
        {
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
