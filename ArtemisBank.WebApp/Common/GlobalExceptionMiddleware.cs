using System.Text.Json;
using ArtemisBank.Core.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBank.WebApp.Common
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger,
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
                    "Error no controlado. Ruta {Path}. Usuario {User}. Correlación {CorrelationId}",
                    context.Request.Path, context.User?.Identity?.Name ?? "anónimo",
                    context.TraceIdentifier);

                if (context.Response.HasStarted) throw;

                if (WantsJson(context))
                {
                    var problem = new ProblemDetails
                    {
                        Status = status,
                        Title = title,
                        Detail = status == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                            ? "Ocurrió un error inesperado. Intente nuevamente."
                            : ex.Message,
                        Instance = context.Request.Path
                    };
                    problem.Extensions["correlationId"] = context.TraceIdentifier;

                    if (ex is AppValidationException validation)
                        problem.Extensions["errors"] = validation.Errors;

                    context.Response.Clear();
                    context.Response.StatusCode = status;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                    return;
                }

                context.Response.Redirect($"/Home/Error?code={status}");
            }
        }

        private static bool WantsJson(HttpContext context)
            => context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
               || (context.Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

        private static (int Status, string Title) Translate(Exception ex) => ex switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Errores de validación."),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Regla de negocio incumplida."),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto de estado."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado."),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
