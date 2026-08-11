using ArtemisBank.Core.Application.ViewModels.Home;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>Indicadores generales del Home del administrador. Dueno: Michael.</summary>
    public interface IAdminHomeService
    {
        Task<AdminHomeViewModel> GetIndicatorsAsync(CancellationToken cancellationToken = default);
    }
}
