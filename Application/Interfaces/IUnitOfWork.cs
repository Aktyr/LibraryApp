namespace LibApp.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity;

    /// <summary>
    /// Выполняет действие в транзакции. Автоматически коммитит при успехе и откатывает при исключении.
    /// Если транзакция уже открыта (вложенный вызов) — просто выполняет действие без создания новой.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}