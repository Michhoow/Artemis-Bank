using System.Security.Claims;
using Serilog.Context;

namespace ArtemisBank.WebApi.Common
{
    /// <summary>
    /// Enriquece cada log del request con la informacion exigida por el requerimiento tecnico:
    /// usuario autenticado, rol, endpoint ejecutado e identificador de correlacion.
    /// Nunca agrega contrasenias, tokens ni datos de tarjetas.
    /// </summary>
    public class RequestAuditMiddleware
    {
        public const string CorrelationHeader = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public RequestAuditMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var provided)
                                && !string.IsNullOrWhiteSpace(provided)
                ? provided.ToString()
                : context.TraceIdentifier;

            context.Response.Headers[CorrelationHeader] = correlationId;

            var user = context.User;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("Usuario", user?.Identity?.Name ?? "anónimo"))
            using (LogContext.PushProperty("UsuarioId",
                       user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("uid") ?? "-"))
            using (LogContext.PushProperty("Rol", user?.FindFirstValue(ClaimTypes.Role) ?? "-"))
            using (LogContext.PushProperty("Endpoint",
                       $"{context.Request.Method} {context.Request.Path}"))
            {
                await _next(context);
            }
        }
    }

    public static class RequestAuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestAudit(this IApplicationBuilder app)
            => app.UseMiddleware<RequestAuditMiddleware>();
    }
}
