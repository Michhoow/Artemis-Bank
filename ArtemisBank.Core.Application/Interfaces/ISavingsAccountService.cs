using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Dtos.Transactions;
using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ISavingsAccountService : IGenericService<SavingsAccountDto>
    {
        Task<PagedResult<SavingsAccountDto>> GetPagedAsync(SavingsAccountFilterDto filter,
            CancellationToken cancellationToken = default);

        Task<(PagedResult<SavingsAccountDto> Result, string? InfoMessage)> SearchAsync(
            SavingsAccountFilterDto filter, CancellationToken cancellationToken = default);

        Task<SavingsAccountDto?> GetByAccountNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default);

        Task<SavingsAccountTransactionsDto> GetTransactionsAsync(string accountNumber, int page, int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<SavingsAccountDto>> GetActiveByClientAsync(string clientId,
            CancellationToken cancellationToken = default);

        Task<SavingsAccountDto> CreateSecondaryAsync(string clientId, decimal initialBalance,
            string? createdByUserId, CancellationToken cancellationToken = default);

        Task CancelSecondaryAsync(string accountNumber, string? cancelledByUserId,
            CancellationToken cancellationToken = default);

        Task<SavingsAccount?> GetEntityByNumberAsync(string accountNumber,
            CancellationToken cancellationToken = default);

        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        Task<List<TransactionDto>> GetLastTransactionsAsync(int accountId, int take,
            CancellationToken cancellationToken = default);
    }
}
