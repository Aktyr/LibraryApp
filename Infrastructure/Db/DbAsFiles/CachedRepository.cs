using LibApp.Core.Records;

namespace LibApp.Infrastructure.Db.DbAsFiles;

public class CachedRepository<T> : IRepository<T> where T : class, IEntity, new()
{
    private readonly IRepository<T> _decorated;
    private ICollection<T> _cache;
    private bool _isDirty = true;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public CachedRepository(IRepository<T> decorated)
    {
        _decorated = decorated;
    }

    public async Task AddRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        await _decorated.AddRange(entities, cancellationToken);
        await MarkDirtyAsync();
    }

    public async Task UpdateRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        await _decorated.UpdateRange(entities, cancellationToken);
        await MarkDirtyAsync();
    }

    public async Task RemoveRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        await _decorated.RemoveRange(entities, cancellationToken);
        await MarkDirtyAsync();
    }

    public async Task<IEnumerable<T>> Get(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_isDirty)
            {
                _cache = (await _decorated.Get(cancellationToken)).ToList();
                _isDirty = false;
            }
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T?> Get(Id id, CancellationToken cancellationToken)
    {
        var items = await Get(cancellationToken);
        return items.FirstOrDefault(x => x.Id.Value == id.Value);
    }

    public async Task<IEnumerable<T>> Get(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        var items = await Get(cancellationToken);
        return items.Where(predicate);
    }

    public async Task<IEnumerable<T>> GetWithoutTracking(CancellationToken cancellationToken)
    {
        // Для кэшированного репозитория всегда возвращаем кэш
        return await Get(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetWithoutTracking(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        var items = await Get(cancellationToken);
        return items.Where(predicate);
    }

    private async Task MarkDirtyAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _isDirty = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    #region Дополнительные методы для управления кешем
    public async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cache = (await _decorated.Get(cancellationToken)).ToList();
            _isDirty = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsCacheValidAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return !_isDirty;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task InvalidateCacheAsync() => await MarkDirtyAsync();

    public void Dispose()
    {
        _lock?.Dispose();
    }
    #endregion
}