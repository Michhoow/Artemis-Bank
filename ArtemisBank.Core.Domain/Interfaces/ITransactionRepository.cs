using ArtemisBank.Core.Domain.Entities;

namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        IQueryable<Transaction> GetByAccountQuery(int savingsAccountId);
        Task<int> CountDistinctOperationsAsync(DateTime? day = null);
        Task<int> CountDistinctPaymentsAsync(DateTime? day = null);
        Task<int> CountByCashierAsync(string cashierUserId, DateTime day);
        Task<int> CountPaymentsByCashierAsync(string cashierUserId, DateTime day);
        Task<int> CountByCashierAndOperationAsync(
            string cashierUserId, Common.Enums.TransactionOperation operation, DateTime day);
    }
}
