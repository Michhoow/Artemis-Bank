using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Commerces;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ICommerceService : IGenericService<CommerceDto>
    {
        Task<PagedResult<CommerceDto>> GetPagedAsync(int page, int pageSize, string? status = null,
            CancellationToken cancellationToken = default);

        Task<CommerceDetailDto?> GetDetailAsync(int commerceId, CancellationToken cancellationToken = default);

        Task<CommerceDto?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<CommerceDto> CreateAsync(SaveCommerceDto request, string? adminUserId,
            CancellationToken cancellationToken = default);

        Task<CommerceDto> UpdateAsync(int commerceId, SaveCommerceDto request,
            CancellationToken cancellationToken = default);

        Task<CommerceDto> SetStatusAsync(int commerceId, bool isActive, string? adminUserId,
            CancellationToken cancellationToken = default);
    }
}
