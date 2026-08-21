using ArtemisBank.Core.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.AzureFunctions.Functions
{
    public class OverdueInstallmentsFunction
    {
        private readonly IOverdueInstallmentProcessor _processor;
        private readonly ILogger<OverdueInstallmentsFunction> _logger;

        public OverdueInstallmentsFunction(
            IOverdueInstallmentProcessor processor,
            ILogger<OverdueInstallmentsFunction> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        [Function("MarcarCuotasAtrasadas")]
        public async Task RunAsync(
            [TimerTrigger("0 0 1 * * *")] TimerInfo timer,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Inicio del proceso diario de cuotas atrasadas.");

            try
            {
                var result = await _processor.RunAsync(DateTime.Now, cancellationToken);
                _logger.LogInformation("Proceso completado. {Summary}", result.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el proceso diario de cuotas atrasadas.");
                throw;
            }

            if (timer.ScheduleStatus is not null)
                _logger.LogInformation("Próxima ejecución: {Next}", timer.ScheduleStatus.Next);
        }
    }
}
