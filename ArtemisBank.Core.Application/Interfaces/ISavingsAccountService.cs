using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Dtos.Transactions;
using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Application.Interfaces
{
    /// <summary>
    /// Servicio de negocio de cuentas de ahorro. Dueno: Michael.
    /// Hereda del servicio generico (patron de la solucion) y agrega las operaciones del modulo.
    /// </summary>
    public interface ISavingsAccountService : IGenericService<SavingsAccountDto>
    {
        Task<PagedResult<SavingsAccountDto>> GetPagedAsync(SavingsAccountFilterDto filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Igual que GetPagedAsync pero devuelve ademas el mensaje informativo exigido por el
        /// documento funcional ("No existe un cliente..." / "Este cliente no tiene cuentas...").
        /// </summary>
        Task<(PagedResult<SavingsAccountDto> Result, string? InfoMessage)> SearchAsync(
            SavingsAccountFilterDto filter, CancellationToken cancellationToken = default);

        Task<SavingsAccountDto?> GetByAccountNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default);

        Task<SavingsAccountTransactionsDto> GetTransactionsAsync(string accountNumber, int page, int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Cuentas activas del cliente: principal primero, secundarias de mayor a menor balance.</summary>
        Task<List<SavingsAccountDto>> GetActiveByClientAsync(string clientId,
            CancellationToken cancellationToken = default);

        /// <summary>Crea una cuenta SECUNDARIA. Registra CREDITO inicial si el balance es mayor que cero.</summary>
        Task<SavingsAccountDto> CreateSecondaryAsync(string clientId, decimal initialBalance,
            string? createdByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela una cuenta secundaria activa. Si tiene balance lo transfiere a la principal
        /// con registro cruzado DEBITO/CREDITO antes de cambiar el estado.
        /// </summary>
        Task CancelSecondaryAsync(string accountNumber, string? cancelledByUserId,
            CancellationToken cancellationToken = default);

        /// <summary>Entidad cruda, para uso interno de otros servicios del equipo.</summary>
        Task<SavingsAccount?> GetEntityByNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        Task<List<TransactionDto>> GetLastTransactionsAsync(int accountId, int take,
            CancellationToken cancellationToken = default);
    }
}
