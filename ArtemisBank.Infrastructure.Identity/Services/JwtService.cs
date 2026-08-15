using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArtemisBank.Core.Domain.Settings;
using ArtemisBank.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    /// <summary>
    /// Genera tokens JWT para la Web API.
    /// Claims incluidos (DoD MON-03): sub/uid (id), unique_name (username), role, jti, exp.
    /// El token expira según <see cref="JwtSettings.DurationInMinutes"/> (default 60 min).
    /// NUNCA se loguean el token completo ni la signing key.
    /// </summary>
    public class JwtService
    {
        private readonly JwtSettings _settings;
        private readonly UserManager<AppUser> _userManager;

        public JwtService(IOptions<JwtSettings> settings, UserManager<AppUser> userManager)
        {
            _settings    = settings.Value;
            _userManager = userManager;
        }

        /// <summary>
        /// Genera un JWT firmado para el <paramref name="user"/> dado.
        /// </summary>
        /// <returns>Token serializado como string.</returns>
        public async Task<string> GenerateTokenAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role  = roles.FirstOrDefault() ?? string.Empty;

            var claims = new List<Claim>
            {
                // Claim estándar de sujeto + uid custom (legible por HttpContextAuthenticatedUser)
                new(JwtRegisteredClaimNames.Sub,            user.Id),
                new("uid",                                  user.Id),
                new(JwtRegisteredClaimNames.UniqueName,     user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Email,          user.Email    ?? string.Empty),
                new(ClaimTypes.NameIdentifier,              user.Id),
                new(ClaimTypes.Name,                        user.UserName ?? string.Empty),
                new(ClaimTypes.Role,                        role),
                // jti para invalidación futura y trazabilidad
                new(JwtRegisteredClaimNames.Jti,            Guid.NewGuid().ToString()),
            };

            var keyBytes      = Encoding.UTF8.GetBytes(GetSigningKey());
            var signingKey    = new SymmetricSecurityKey(keyBytes);
            var signingCreds  = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiration    = DateTime.UtcNow.AddMinutes(_settings.DurationInMinutes > 0
                                    ? _settings.DurationInMinutes : 60);

            var token = new JwtSecurityToken(
                issuer:             _settings.Issuer,
                audience:           _settings.Audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            expiration,
                signingCredentials: signingCreds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Devuelve la clave de firma con fallback de desarrollo.</summary>
        private string GetSigningKey() =>
            string.IsNullOrWhiteSpace(_settings.Key)
                ? "clave-de-desarrollo-artemis-banking-pro-2026-cambiar"
                : _settings.Key;
    }
}
