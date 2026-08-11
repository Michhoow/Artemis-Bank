using ArtemisBank.Core.Application.ViewModels.Beneficiaries;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>Beneficiarios frecuentes del cliente. Dueno: Michael.</summary>
    public interface IBeneficiaryService
    {
        Task<List<BeneficiaryViewModel>> GetByClientAsync(string clientId,
            CancellationToken cancellationToken = default);

        Task AddAsync(string clientId, string accountNumber, CancellationToken cancellationToken = default);

        Task DeleteAsync(string clientId, int beneficiaryId, CancellationToken cancellationToken = default);

        Task<BeneficiaryViewModel?> GetByIdAsync(string clientId, int beneficiaryId,
            CancellationToken cancellationToken = default);
    }
}
