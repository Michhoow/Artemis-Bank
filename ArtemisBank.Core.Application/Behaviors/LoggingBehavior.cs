using MediatR;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var name = typeof(TRequest).Name;
            var startedAt = DateTime.Now;

            _logger.LogInformation("Ejecutando {RequestName}", name);

            try
            {
                var response = await next();
                _logger.LogInformation("{RequestName} completado en {Elapsed} ms",
                    name, (DateTime.Now - startedAt).TotalMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{RequestName} finalizo con error", name);
                throw;
            }
        }
    }
}
