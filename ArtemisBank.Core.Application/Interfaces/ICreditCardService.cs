using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ICreditCardService : IGenericService<CreditCardDto>
    {
        Task<PagedResult<CreditCardDto>> GetPagedAsync(CreditCardFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<CreditCardDetailDto?> GetDetailAsync(int creditCardId,
            CancellationToken cancellationToken = default);

        Task<List<CreditCardDto>> GetByClientAsync(string clientId, bool onlyActive = true,
            CancellationToken cancellationToken = default);

        Task<CreatedCreditCardDto> CreateAsync(CreateCreditCardDto request, string? adminUserId,
            CancellationToken cancellationToken = default);

        Task<CreditCardDto> UpdateLimitAsync(int creditCardId, decimal newLimit, string? adminUserId,
            CancellationToken cancellationToken = default);

        Task CancelAsync(int creditCardId, string? adminUserId,
            CancellationToken cancellationToken = default);

        Task<CashAdvanceResultDto> CashAdvanceAsync(CashAdvanceRequestDto request, string clientId,
            CancellationToken cancellationToken = default);
    }
}
