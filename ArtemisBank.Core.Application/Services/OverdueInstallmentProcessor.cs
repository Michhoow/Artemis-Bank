using ArtemisBank.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class OverdueRunResult
    {
        public DateTime ExecutedAt { get; set; }
        public int InstallmentsMarkedOverdue { get; set; }
        public int InstallmentsCleared { get; set; }

        public override string ToString() =>
            $"Cuotas marcadas como atrasadas: {InstallmentsMarkedOverdue}. " +
            $"Cuotas que dejaron de estar atrasadas: {InstallmentsCleared}.";
    }

    public interface IOverdueInstallmentProcessor
    {
        Task<OverdueRunResult> RunAsync(DateTime asOf, CancellationToken cancellationToken = default);
    }

    public class OverdueInstallmentProcessor : IOverdueInstallmentProcessor
    {
        private readonly ILoanInstallmentRepository _installmentRepository;
        private readonly ILogger<OverdueInstallmentProcessor> _logger;

        public OverdueInstallmentProcessor(
            ILoanInstallmentRepository installmentRepository,
            ILogger<OverdueInstallmentProcessor> logger)
        {
            _installmentRepository = installmentRepository;
            _logger = logger;
        }

        public async Task<OverdueRunResult> RunAsync(DateTime asOf,
            CancellationToken cancellationToken = default)
        {
            var result = new OverdueRunResult { ExecutedAt = asOf };

            var overdue = await _installmentRepository.GetOverdueAsync(asOf.Date, cancellationToken);

            foreach (var installment in overdue)
            {
                installment.IsOverdue = true;
                await _installmentRepository.UpdateEntityAsync(installment);
                result.InstallmentsMarkedOverdue++;
            }

            var cleared = await ClearSettledAsync(cancellationToken);
            result.InstallmentsCleared = cleared;

            _logger.LogInformation(
                "Proceso de mora ejecutado el {ExecutedAt:yyyy-MM-dd}. {Summary}",
                asOf, result.ToString());

            return result;
        }

        private async Task<int> ClearSettledAsync(CancellationToken cancellationToken)
        {
            var all = await _installmentRepository.GetAllListAsync();

            var settledButFlagged = all
                .Where(i => i.IsOverdue && i.PaidAmount >= i.TotalAmount)
                .ToList();

            foreach (var installment in settledButFlagged)
            {
                installment.IsOverdue = false;
                await _installmentRepository.UpdateEntityAsync(installment);
            }

            return settledButFlagged.Count;
        }
    }
}
