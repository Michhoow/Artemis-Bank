using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Controllers
{
    /// <summary>Utilidades comunes a los controladores: usuario autenticado y mensajes de UI.</summary>
    public abstract class BaseController : Controller
    {
        protected readonly IAuthenticatedUser CurrentUser;

        protected BaseController(IAuthenticatedUser currentUser) => CurrentUser = currentUser;

        protected string UserId => CurrentUser.UserId ?? string.Empty;

        protected OperationActor ActorAs(Roles role) => OperationActor.Of(UserId, role);

        protected void Success(string message) => TempData["Success"] = message;

        protected void Error(string message) => TempData["Error"] = message;

        protected void Warning(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message)) TempData["Warning"] = message;
        }
    }
}
