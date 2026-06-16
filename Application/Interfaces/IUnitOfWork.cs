namespace LibApp.Application.Interfaces;

// todo реализовать транзакции для команд
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity;
}