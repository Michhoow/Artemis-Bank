using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ArtemisDbContext context) : base(context) { }

        public IQueryable<Transaction> GetByAccountQuery(int savingsAccountId)
            => Context.Transactions.Where(t => t.SavingsAccountId == savingsAccountId).AsQueryable();

        /// <summary>
        /// Cuenta OPERACIONES, no filas: una transferencia registra dos movimientos
        /// (debito y credito) que comparten OperationReference y representan una sola transaccion.
        /// El documento pide las transacciones "registradas", por lo que incluye aprobadas y rechazadas.
        /// </summary>
        public async Task<int> CountDistinctOperationsAsync(DateTime? day = null)
        {
            var query = Context.Transactions.AsQueryable();

            if (day.HasValue)
            {
                var start = day.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(t => t.CreatedAt >= start && t.CreatedAt < end);
            }

            return await query.Select(t => t.OperationReference).Distinct().CountAsync();
        }

        /// <summary>
        /// Solo pagos a tarjetas de credito y pagos a prestamos, y solo los procesados correctamente.
        /// No cuenta depositos, retiros, transferencias ni avances de efectivo.
        /// </summary>
        public async Task<int> CountDistinctPaymentsAsync(DateTime? day = null)
        {
            var query = Context.Transactions.Where(t =>
                t.Status == TransactionStatus.Aprobada &&
                (t.Operation == TransactionOperation.PagoTarjeta ||
                 t.Operation == TransactionOperation.PagoPrestamo));

            if (day.HasValue)
            {
                var start = day.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(t => t.CreatedAt >= start && t.CreatedAt < end);
            }

            return await query.Select(t => t.OperationReference).Distinct().CountAsync();
        }

        /// <summary>
        /// Operaciones del dia realizadas por un cajero especifico.
        /// Incluye depositos, retiros, pagos a tarjeta, pagos a prestamo y transacciones a terceros.
        /// </summary>
        public async Task<int> CountByCashierAsync(string cashierUserId, DateTime day)
        {
            var start = day.Date;
            var end = start.AddDays(1);

            return await Context.Transactions
                .Where(t => t.PerformedByUserId == cashierUserId
                            && t.PerformedByRole == Roles.Cajero
                            && t.Status == TransactionStatus.Aprobada
                            && t.CreatedAt >= start && t.CreatedAt < end)
                .Select(t => t.OperationReference)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> CountPaymentsByCashierAsync(string cashierUserId, DateTime day)
        {
            var start = day.Date;
            var end = start.AddDays(1);

            return await Context.Transactions
                .Where(t => t.PerformedByUserId == cashierUserId
                            && t.PerformedByRole == Roles.Cajero
                            && t.Status == TransactionStatus.Aprobada
                            && (t.Operation == TransactionOperation.PagoTarjeta ||
                                t.Operation == TransactionOperation.PagoPrestamo)
                            && t.CreatedAt >= start && t.CreatedAt < end)
                .Select(t => t.OperationReference)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> CountByCashierAndOperationAsync(string cashierUserId,
            TransactionOperation operation, DateTime day)
        {
            var start = day.Date;
            var end = start.AddDays(1);

            return await Context.Transactions
                .Where(t => t.PerformedByUserId == cashierUserId
                            && t.PerformedByRole == Roles.Cajero
                            && t.Status == TransactionStatus.Aprobada
                            && t.Operation == operation
                            && t.CreatedAt >= start && t.CreatedAt < end)
                .Select(t => t.OperationReference)
                .Distinct()
                .CountAsync();
        }
    }
}
