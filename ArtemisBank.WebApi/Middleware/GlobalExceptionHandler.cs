using System.Text.Json;
using ArtemisBank.Core.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApi.Middleware
{
    /// <summary>
    /// Manejo centralizado de errores de la API.
    /// Todas las respuestas de error siguen el estandar Problem Details (RFC 7807)
    /// e incluyen un identificador de correlacion. Nunca exponen datos sensibles ni trazas internas.
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var (status, title) = Translate(ex);

                _logger.LogError(ex,
                    "Error no controlado. {Method} {Path}. Usuario {User}. Rol {Role}. Correlación {CorrelationId}",
                    context.Request.Method, context.Request.Path,
                    context.User?.Identity?.Name ?? "anónimo",
                    context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "-",
                    context.TraceIdentifier);

                if (context.Response.HasStarted) throw;

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Type = $"https://httpstatuses.io/{status}",
                    Instance = context.Request.Path,
                    Detail = status == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                        ? "Ocurrió un error inesperado al procesar la solicitud."
                        : ex.Message
                };
                problem.Extensions["correlationId"] = context.TraceIdentifier;

                if (ex is AppValidationException validation)
                    problem.Extensions["errors"] = validation.Errors;

                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
        }

        private static (int Status, string Title) Translate(Exception ex) => ex switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Errores de validación."),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Regla de negocio incumplida."),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto de estado."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autenticado."),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };
    }

    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionHandler>();
    }
}
