using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    public class RiskAssessmentService : IRiskAssessmentService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IUserReadService _userReadService;

        public RiskAssessmentService(
            ILoanRepository loanRepository,
            ICreditCardRepository creditCardRepository,
            IUserReadService userReadService)
        {
            _loanRepository = loanRepository;
            _creditCardRepository = creditCardRepository;
            _userReadService = userReadService;
        }

        public async Task<decimal> GetClientDebtAsync(string clientId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return 0m;

            var loanDebt = await _loanRepository.GetPendingDebtByClientAsync(clientId, cancellationToken);
            var cardDebt = await _creditCardRepository.GetDebtByClientAsync(clientId, cancellationToken);

            return Money.Round(loanDebt + cardDebt);
        }

        public async Task<decimal> GetAverageActiveClientDebtAsync(
            CancellationToken cancellationToken = default)
        {
            var activeClientIds = await _userReadService.GetActiveClientIdsAsync();

            if (activeClientIds == null || activeClientIds.Count == 0) return 0m;

            var total = 0m;
            foreach (var clientId in activeClientIds)
                total += await GetClientDebtAsync(clientId, cancellationToken);

            return Money.Round(total / activeClientIds.Count);
        }

        public async Task<HighRiskEvaluationDto> EvaluateAsync(string clientId, decimal newLoanTotalToPay,
            CancellationToken cancellationToken = default)
        {
            var currentDebt = await GetClientDebtAsync(clientId, cancellationToken);
            var averageDebt = await GetAverageActiveClientDebtAsync(cancellationToken);
            var projectedDebt = Money.Round(currentDebt + Money.Round(newLoanTotalToPay));

            var evaluation = new HighRiskEvaluationDto
            {
                CurrentDebt = currentDebt,
                ProjectedDebt = projectedDebt,
                AverageDebt = averageDebt
            };

            if (currentDebt > averageDebt)
            {
                evaluation.IsHighRisk = true;
                evaluation.RiskType = "CurrentHighRisk";
                evaluation.Message = AppMessages.LoanCurrentHighRisk;
                return evaluation;
            }

            if (projectedDebt > averageDebt)
            {
                evaluation.IsHighRisk = true;
                evaluation.RiskType = "ProjectedHighRisk";
                evaluation.Message = AppMessages.LoanProjectedHighRisk;
                return evaluation;
            }

            evaluation.IsHighRisk = false;
            evaluation.RiskType = "None";
            evaluation.Message = string.Empty;
            return evaluation;
        }
    }
}
