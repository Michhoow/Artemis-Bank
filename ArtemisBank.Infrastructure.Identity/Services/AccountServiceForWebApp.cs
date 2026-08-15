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
    /// <summary>
    /// Servicio de cuenta para la WebApp MVC.
    /// Gestiona login/logout con cookies, confirmación de cuenta por token
    /// y reset de contraseña de un solo uso.
    /// Reglas de negocio (DoD MON-01 + MON-03):
    ///   - Login solo si usuario activo Y credenciales válidas.
    ///   - Acceso directo bloqueado por rol vía [Authorize(Roles=...)].
    ///   - Nuevo usuario → IsActive = false; se activa por correo de confirmación.
    /// </summary>
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

        // ── Login ────────────────────────────────────────────────────────────

        /// <summary>
        /// Autentica al usuario con cookie de sesión.
        /// Devuelve (true, null) en éxito o (false, mensaje) en fallo.
        /// </summary>
        public async Task<(bool Success, string? Error)> LoginAsync(
            string userName, string password, string[] allowedRoles)
        {
            var user = await _userManager.FindByNameAsync(userName)
                    ?? await _userManager.FindByEmailAsync(userName);

            if (user is null)
                return (false, "Credenciales inválidas.");

            if (!user.IsActive)
                return (false, "Su cuenta no está activa. Revise su correo de activación.");

            // Verificar rol permitido
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                return (false, "No tiene permiso para acceder con este rol.");

            // Verificar contraseña SIN actualizar SecurityStamp (no SignInAsync todavía)
            var passwordOk = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordOk)
            {
                _logger.LogWarning("Login fallido para '{UserName}': contraseña incorrecta.", userName);
                return (false, "Credenciales inválidas.");
            }

            // Emitir cookie manualmente para controlar exactamente los claims
            var role = roles.First(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            var claimsPrincipal = BuildClaimsPrincipal(user, role);

            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No hay HttpContext disponible.");

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent    = false,
                    ExpiresUtc      = DateTimeOffset.UtcNow.AddMinutes(60)
                });

            _logger.LogInformation("Login exitoso: usuario '{UserName}', rol '{Role}'.", userName, role);
            return (true, null);
        }

        /// <summary>Cierra la sesión del usuario actual.</summary>
        public async Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Logout ejecutado.");
        }

        // ── Registro y activación ────────────────────────────────────────────

        /// <summary>
        /// Crea un usuario inactivo, lo asigna al rol indicado y envía correo de activación.
        /// El usuario nace con <c>IsActive = false</c> hasta que se confirme el correo.
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
            // Unicidad de cédula
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
                IsActive       = false   // Regla: usuarios nuevos nacen inactivos
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return (false, JoinErrors(createResult), null);

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user); // rollback
                return (false, $"No se pudo asignar el rol: {JoinErrors(roleResult)}", null);
            }

            // Crear cuenta principal automática si es Cliente
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

            // Generar token de confirmación y enviar correo
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendActivationEmailAsync(user, token);

            _logger.LogInformation("Usuario '{UserName}' creado, inactivo. Correo de activación enviado.", userName);
            return (true, null, user);
        }

        /// <summary>
        /// Confirma el correo del usuario y lo activa.
        /// Token de un solo uso (ASP.NET Identity lo invalida tras el primer uso).
        /// </summary>
        public async Task<(bool Success, string? Error)> ConfirmEmailAsync(
            string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return (false, "Usuario no encontrado.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return (false, JoinErrors(result));

            // Activar la cuenta al confirmar
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Cuenta activada para usuario '{UserId}'.", userId);
            return (true, null);
        }

        // ── Reset de contraseña ──────────────────────────────────────────────

        /// <summary>
        /// Genera un token de reset de contraseña válido por 30 min y lo envía por correo.
        /// Responde igual independientemente de si el usuario existe (para no exponer existencia).
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                // Respuesta silenciosa — no revela si el correo existe
                _logger.LogInformation("Reset solicitado para email '{Email}': no encontrado (respuesta silenciosa).", email);
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await SendResetPasswordEmailAsync(user, token);
            _logger.LogInformation("Token de reset enviado al email '{Email}'.", email);
        }

        /// <summary>
        /// Aplica el nuevo password. Token de un solo uso, expira en 30 min.
        /// </summary>
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

        // ── Helpers privados ─────────────────────────────────────────────────

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
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        private async Task SendActivationEmailAsync(AppUser user, string token)
        {
            // En la WebApp el enlace incluye el token como query string.
            // El controlador de Account construirá la URL completa.
            // Aquí se envía el token directamente para que el controlador componga la URL.
            var body = $"""
                <h2>Bienvenido a Artemis Banking Pro</h2>
                <p>Hola {user.FirstName},</p>
                <p>Su cuenta ha sido creada. Use el siguiente código para activarla:</p>
                <p><strong>{token}</strong></p>
                <p>Si no solicitó esta cuenta, ignore este correo.</p>
                """;

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Activación de cuenta",
                HtmlBody = body
            });
        }

        private async Task SendResetPasswordEmailAsync(AppUser user, string token)
        {
            var body = $"""
                <h2>Artemis Banking Pro — Restablecimiento de contraseña</h2>
                <p>Hola {user.FirstName},</p>
                <p>Se solicitó un restablecimiento de contraseña. Use el siguiente código:</p>
                <p><strong>{token}</strong></p>
                <p>Este código expira en <strong>30 minutos</strong> y es de un solo uso.</p>
                <p>Si no lo solicitó, ignore este correo.</p>
                """;

            await _emailService.SendAsync(new EmailRequest
            {
                To       = user.Email ?? string.Empty,
                Subject  = "Artemis Banking Pro — Restablecer contraseña",
                HtmlBody = body
            });
        }

        private static string JoinErrors(IdentityResult result)
            => string.Join("; ", result.Errors.Select(e => e.Description));
    }

    // Extensión auxiliar para usar FirstOrDefaultAsync sin LINQ-to-EF en este contexto
    file static class QueryableExtensions
    {
        internal static Task<AppUser?> FirstOrDefaultAsync(
            this IQueryable<AppUser> query,
            System.Linq.Expressions.Expression<Func<AppUser, bool>> predicate)
            => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(query, predicate);
    }
}
