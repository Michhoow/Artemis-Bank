using ArtemisBank.Infrastructure.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Controllers.v1
{
    public class LoginRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ConfirmAccountRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class GetResetTokenRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Endpoints públicos de autenticación e identidad de la API.
    /// Propiedad: Monserrat (DoD MON-03).
    /// </summary>
    [AllowAnonymous]
    public class AccountController : BaseApiController
    {
        private readonly IAccountServiceForWebApi _accountService;

        public AccountController(IAccountServiceForWebApi accountService)
        {
            _accountService = accountService;
        }

        /// <summary>Autenticación con usuario/email y clave. Retorna el JWT si la cuenta está activa.</summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (token, error) = await _accountService.LoginAsync(request.UserName, request.Password);
            if (!string.IsNullOrEmpty(error))
                return Unauthorized(new { error });

            return Ok(new { token });
        }

        /// <summary>Confirma la cuenta con el token de activación enviada por correo (un solo uso).</summary>
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAccountRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error) = await _accountService.ConfirmAccountAsync(request.UserId, request.Token);
            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = "Cuenta activada exitosamente." });
        }

        /// <summary>Solicita token de restablecimiento de contraseña. El token se envía por correo en el cuerpo.</summary>
        [HttpPost("get-reset-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (token, userId, error) = await _accountService.GetResetTokenAsync(request.Email);
            if (token is null)
                return NoContent(); // Respuesta silenciosa por seguridad

            return Ok(new { message = "Token enviado al correo electrónico.", userId, token });
        }

        /// <summary>Aplica la nueva contraseña usando el token de un solo uso (30 min).</summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error) = await _accountService.ResetPasswordAsync(
                request.UserId, request.Token, request.NewPassword);

            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = "Contraseña restablecida exitosamente." });
        }
    }
}
