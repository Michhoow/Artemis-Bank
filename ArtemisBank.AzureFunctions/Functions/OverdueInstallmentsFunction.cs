using ArtemisBank.Core.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.AzureFunctions.Functions
{
    public class OverdueInstallmentsFunction
    {
        private readonly ILoanService _loanService;
        private readonly ILogger<OverdueInstallmentsFunction> _logger;

        public OverdueInstallmentsFunction(ILoanService loanService, ILogger<OverdueInstallmentsFunction> logger)
        {
            _loanService = loanService;
            _logger = logger;
        }

        [Function("OverdueInstallmentsFunction")]
        public async Task RunAsync([TimerTrigger("0 5 * * *")] TimerInfo timer, CancellationToken cancellationToken)
        {
            var asOf = DateTime.Now;
            _logger.LogInformation("Proceso de mora iniciado a las {Start}.", asOf);

            try
            {
                var changed = await _loanService.RefreshOverdueInstallmentsAsync(asOf, cancellationToken);
                _logger.LogInformation("Proceso de mora finalizado. {Count} cuotas actualizadas.", changed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "El proceso de mora fallo. Se reintentara en la proxima ejecucion programada.");
                throw;
            }

            if (timer.ScheduleStatus is not null)
                _logger.LogInformation("Proxima ejecucion programada: {Next}.", timer.ScheduleStatus.Next);
        }
    }
}
