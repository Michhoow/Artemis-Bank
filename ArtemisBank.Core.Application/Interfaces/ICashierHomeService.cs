using ArtemisBank.Core.Application.ViewModels.Home;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>Indicadores del dia del cajero autenticado. Dueno: Michael.</summary>
    public interface ICashierHomeService
    {
        Task<CashierHomeViewModel> GetIndicatorsAsync(string cashierUserId,
            CancellationToken cancellationToken = default);
    }
}
