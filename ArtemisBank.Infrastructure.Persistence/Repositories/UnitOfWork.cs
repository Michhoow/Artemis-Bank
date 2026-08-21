using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    internal sealed class RelationalAppTransaction : IAppTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _finished;

        public RelationalAppTransaction(IDbContextTransaction transaction) => _transaction = transaction;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
            _finished = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_finished) return;
            await _transaction.RollbackAsync(cancellationToken);
            _finished = true;
        }

        public async ValueTask DisposeAsync() => await _transaction.DisposeAsync();
    }

    internal sealed class NoOpAppTransaction : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ArtemisDbContext _context;

        public UnitOfWork(ArtemisDbContext context) => _context = context;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);

        public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (!_context.Database.IsRelational())
                return new NoOpAppTransaction();

            if (_context.Database.CurrentTransaction != null)
                return new NoOpAppTransaction();

            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new RelationalAppTransaction(transaction);
        }
    }
}
