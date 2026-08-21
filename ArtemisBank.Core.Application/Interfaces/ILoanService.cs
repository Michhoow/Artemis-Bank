using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Loans;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ILoanService : IGenericService<LoanDto>
    {
        Task<PagedResult<LoanDto>> GetPagedAsync(LoanFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<LoanDetailDto?> GetDetailAsync(int loanId, CancellationToken cancellationToken = default);

        Task<LoanDetailDto?> GetDetailByNumberAsync(string loanNumber,
            CancellationToken cancellationToken = default);

        Task<List<LoanDto>> GetByClientAsync(string clientId, bool onlyActive = true,
            CancellationToken cancellationToken = default);

        Task<HighRiskEvaluationDto> EvaluateRiskAsync(CreateLoanDto request,
            CancellationToken cancellationToken = default);

        Task<LoanDetailDto> CreateAsync(CreateLoanDto request, string? adminUserId,
            CancellationToken cancellationToken = default);

        Task<LoanDetailDto> UpdateRateAsync(int loanId, decimal newAnnualRate, string? adminUserId,
            CancellationToken cancellationToken = default);
    }
}
