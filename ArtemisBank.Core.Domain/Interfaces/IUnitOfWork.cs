namespace ArtemisBank.Core.Domain.Interfaces
{
    public interface IAppTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
