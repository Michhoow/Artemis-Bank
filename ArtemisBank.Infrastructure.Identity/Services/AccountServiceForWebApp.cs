using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Infrastructure.Identity.Entities;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApp : IAccountServiceForWebApp
    {
        private readonly UserManager<AppUser>  _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService          _emailService;
        private readonly IHttpContextAccessor   _httpContextAccessor;
        private readonly IAccountNumberGenerator _accountNumberGenerator;
        private readonly ISavingsAccountRepository _savingsAccountRepository;
        private readonly ILogger<AccountServiceForWebApp> _logger;

        public AccountServiceForWebApp(
            UserManager<AppUser>              userManager,
            SignInManager<AppUser>            signInManager,
            IEmailService                     emailService,
            IHttpContextAccessor              httpContextAccessor,
            IAccountNumberGenerator           accountNumberGenerator,
            ISavingsAccountRepository         savingsAccountRepository,
            ILogger<AccountServiceForWebApp>  logger)
        {
            _userManager              = userManager;
            _signInManager            = signInManager;
            _emailService             = emailService;
            _httpContextAccessor      = httpContextAccessor;
            _accountNumberGenerator   = accountNumberGenerator;
            _savingsAccountRepository = savingsAccountRepository;
            _logger                   = logger;
        }

        public async Task<(bool Success, string? Error)> LoginAsync(
            string userName, string password, string[] allowedRoles)
        {
            var user = await _userManager.FindByNameAsync(userName)
                    ?? await _userManager.FindByEmailAsync(userName);

            if (user is null)
                return (false, AppMessages.InvalidCredentials);

            if (!user.IsActive)
                return (false, AppMessages.AccountInactive);

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                if (roles.Any(r => string.Equals(r, "Comercio", StringComparison.OrdinalIgnoreCase)))
                    return (false, AppMessages.CommerceRoleWebAppNotAllowed);

                return (false, AppMessages.AccessDenied);
            }

            var passwordOk = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordOk)
            {
                _logger.LogWarning("Login fallido para '{UserName}': contraseña incorrecta.", userName);
                return (false, AppMessages.InvalidCredentials);
            }

            var role = roles.First(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            var claimsPrincipal = BuildClaimsPrincipal(user, role);

            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No hay HttpContext disponible.");

            await httpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent    = false,
                    ExpiresUtc      = DateTimeOffset.UtcNow.AddMinutes(60)
                });

            _logger.LogInformation("Login exitoso: usuario '{UserName}', rol '{Role}'.", userName, role);
            return (true, null);
        }

        public async Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            _logger.LogInformation("Logout ejecutado.");
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
            var existing = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Identification == identification);
            if (existing is not null)
                return (false, "Ya existe un usuario con esa cédula/identificación.", null);

            var user = new AppUser
            {
                FirstName      = firstName,
                LastName       = lastName,
                Identification = identification,
                Email          = email,
                UserName       = userName,
                IsActive       = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return (false, JoinErrors(createResult), null);

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return (false, $"No se pudo asignar el rol: {JoinErrors(roleResult)}", null);
            }

            if (string.Equals(role, DefaultRoles.Cliente, StringComparison.OrdinalIgnoreCase))
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
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendActivationEmailAsync(user, token);

            _logger.LogInformation("Usuario '{UserName}' creado, inactivo. Correo de activación enviado.", userName);
            return (true, null, user);
        }

        public async Task<(bool Success, string? Error)> ConfirmEmailAsync(
            string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return (false, "Usuario no encontrado.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Cuenta activada para usuario '{UserId}'.", userId);
            return (true, null);
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                _logger.LogInformation("Reset solicitado para email '{Email}': no encontrado (respuesta silenciosa).", email);
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await SendResetPasswordEmailAsync(user, token);
            _logger.LogInformation("Token de reset enviado al email '{Email}'.", email);
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(
            string userId, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return (false, "Usuario no encontrado.");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            _logger.LogInformation("Contraseña restablecida para usuario '{UserId}'.", userId);
            return (true, null);
        }

        private static ClaimsPrincipal BuildClaimsPrincipal(AppUser user, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name,           user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email,          user.Email    ?? string.Empty),
                new Claim(ClaimTypes.Role,           role),
                new Claim("uid",                     user.Id),
                new Claim("firstName",               user.FirstName),
                new Claim("lastName",                user.LastName),
            };
            var identity  = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            return new ClaimsPrincipal(identity);
        }

        private async Task SendActivationEmailAsync(AppUser user, string token)
        {
            var link = BuildLink("/Login/ConfirmAccount", user.Id, token);

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Active su cuenta",
                HtmlBody = BrandedEmail.Activation(user.FirstName, link)
            });
        }

        private async Task SendResetPasswordEmailAsync(AppUser user, string token)
        {
            var link = BuildLink("/Login/ResetPassword", user.Id, token);

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Restablecer contraseña",
                HtmlBody = BrandedEmail.PasswordReset(user.FirstName, link)
            });
        }

        private string BuildLink(string path, string userId, string token)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var origin = request == null ? string.Empty : $"{request.Scheme}://{request.Host}";

            return $"{origin}{path}" +
                   $"?userId={Uri.EscapeDataString(userId)}" +
                   $"&token={Uri.EscapeDataString(token)}";
        }

        private static string JoinErrors(IdentityResult result)
            => string.Join("; ", result.Errors.Select(e => e.Description));
    }

    file static class QueryableExtensions
    {
        internal static Task<AppUser?> FirstOrDefaultAsync(
            this IQueryable<AppUser> query,
            System.Linq.Expressions.Expression<Func<AppUser, bool>> predicate)
            => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(query, predicate);
    }
}
