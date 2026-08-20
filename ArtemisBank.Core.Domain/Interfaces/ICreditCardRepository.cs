using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    /// <summary>Repositorio de tarjetas de credito y sus consumos. Propiedad: Manuel.</summary>
    public interface ICreditCardRepository : IGenericRepository<CreditCard>
    {
        Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);

        /// <summary>Tarjeta con sus consumos cargados (Include Consumptions).</summary>
        Task<CreditCard?> GetByIdWithConsumptionsAsync(int creditCardId, CancellationToken cancellationToken = default);

        Task<List<CreditCard>> GetActiveByClientAsync(string clientId, CancellationToken cancellationToken = default);

        Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>Suma de la deuda de las tarjetas ACTIVAS del cliente.</summary>
        Task<decimal> GetActiveDebtByClientAsync(string clientId, CancellationToken cancellationToken = default);

        /// <summary>Query base para listados paginados. No incluye consumos por defecto.</summary>
        IQueryable<CreditCard> Query();

        /// <summary>Registra un consumo (aprobado o rechazado) en el historial de la tarjeta.</summary>
        Task AddConsumptionAsync(Consumption consumption, CancellationToken cancellationToken = default);

        /// <summary>Consumos APROBADOS de un comercio (transacciones recibidas por Hermes Pay).</summary>
        IQueryable<Consumption> QueryConsumptionsByCommerce(int commerceId);
    }
}
