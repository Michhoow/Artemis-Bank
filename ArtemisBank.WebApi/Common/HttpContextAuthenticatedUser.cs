using System.Security.Claims;
using ArtemisBank.Core.Application.Interfaces;

namespace ArtemisBank.WebApi.Common
{
    public class HttpContextAuthenticatedUser : IAuthenticatedUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextAuthenticatedUser(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public string? UserId => Principal?.FindFirstValue("uid")
                                 ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

        public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    }
}
