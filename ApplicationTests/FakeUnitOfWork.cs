namespace LibApp.ApplicationTests;

public class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];
    private bool _disposed;

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity
    {
        var type = typeof(TEntity);
        if (!_repositories.TryGetValue(type, out object? value))
        {
            value = new FakeRepository<TEntity>();
            _repositories[type] = value;
        }
        return (IRepository<TEntity>)value;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // В фейке ничего не сохраняем, просто возвращаем успех
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Для тестов просто заглушка
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}