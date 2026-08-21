using ArtemisBank.Core.Application.Dtos.HermesPay;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface IHermesPayService
    {
        Task<HermesPayResultDto> ProcessPaymentAsync(HermesPayRequestDto request, int commerceId,
            string? performedByUserId, CancellationToken cancellationToken = default);

        Task<CommerceTransactionsDto> GetTransactionsAsync(int commerceId, int page, int pageSize,
            CancellationToken cancellationToken = default);
    }
}
