using ArtemisBank.Core.Application.ViewModels.Home;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface IAdminHomeService
    {
        Task<AdminHomeViewModel> GetIndicatorsAsync(CancellationToken cancellationToken = default);
    }
}
