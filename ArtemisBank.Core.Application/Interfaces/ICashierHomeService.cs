using ArtemisBank.Core.Application.ViewModels.Home;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ICashierHomeService
    {
        Task<CashierHomeViewModel> GetIndicatorsAsync(string cashierUserId,
            CancellationToken cancellationToken = default);
    }
}
