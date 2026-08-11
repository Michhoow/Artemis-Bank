using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services.Pending
{
    /// <summary>
    /// TEMPORAL — implementacion puente de ILoanReadService.
    /// La definitiva la provee Manuel en el modulo de prestamos.
    /// Devuelve conjuntos vacios para que los indicadores y las pantallas de Michael
    /// funcionen sin prestamos cargados; nunca inventa datos financieros.
    /// </summary>
    public class PendingLoanReadService : ILoanReadService
    {
        private readonly ILogger<PendingLoanReadService> _logger;

        public PendingLoanReadService(ILogger<PendingLoanReadService> logger) => _logger = logger;

        public Task<LoanInfoDto?> GetByIdAsync(int loanId) => Task.FromResult<LoanInfoDto?>(null);

        public Task<LoanInfoDto?> GetByLoanNumberAsync(string loanNumber) => Task.FromResult<LoanInfoDto?>(null);

        public Task<List<LoanInfoDto>> GetActiveByClientAsync(string clientId)
            => Task.FromResult(new List<LoanInfoDto>());

        public Task<int> CountActiveAsync() => Task.FromResult(0);

        public Task<decimal> GetPendingDebtByClientAsync(string clientId) => Task.FromResult(0m);

        public Task<bool> LoanNumberExistsAsync(string number) => Task.FromResult(false);

        public Task ApplyPaymentAsync(int loanId, decimal effectiveAmount, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "El modulo de prestamos aun no esta integrado: no se aplico el abono de {Amount} al prestamo {LoanId}.",
                effectiveAmount, loanId);
            return Task.CompletedTask;
        }
    }
}
