using System.Security.Claims;
using ArtemisBank.Core.Application.Interfaces;

namespace ArtemisBank.WebApp.Common
{
    /// <summary>
    /// Lee el usuario autenticado desde los claims del request.
    /// Sirve tanto para cookies (WebApp) como para JWT (WebApi) porque usa los claims estandar.
    /// </summary>
    public class HttpContextAuthenticatedUser : IAuthenticatedUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextAuthenticatedUser(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? Principal?.FindFirstValue("uid");

        public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

        public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    }
}
