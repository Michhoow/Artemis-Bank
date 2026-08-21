using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface IRiskAssessmentService
    {
        Task<decimal> GetClientDebtAsync(string clientId, CancellationToken cancellationToken = default);

        Task<decimal> GetAverageActiveClientDebtAsync(CancellationToken cancellationToken = default);

        Task<HighRiskEvaluationDto> EvaluateAsync(string clientId, decimal newLoanTotalToPay,
            CancellationToken cancellationToken = default);
    }
}
