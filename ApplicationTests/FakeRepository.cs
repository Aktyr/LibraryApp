namespace LibApp.ApplicationTests;

internal class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    public List<TEntity> Entities { get; init; } = [];

    #region IRepository

    #region CRUD
    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => Task.Run(() => { Entities.AddRange(entities); }, cancellationToken);

    public Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => Task.Run(() => Entities.RemoveAll(x => entities.Contains(x)));

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            foreach (var entity in entities)
            {
                var existingIndex = Entities.FindIndex(e => e.Id.Value == entity.Id.Value);
                if (existingIndex >= 0)
                {
                    Entities[existingIndex] = entity;
                }
                else
                {
                    Entities.Add(entity);
                }
            }
        }, cancellationToken);
    }
    public Task<IEnumerable<TEntity>> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Entities.AsEnumerable());

    public Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var func = predicate.Compile();
        return Task.FromResult(Entities.Where(func).AsEnumerable());
    }

    public Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(CancellationToken cancellationToken = default)
        => GetAsync(cancellationToken);
    public Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => GetAsync(predicate, cancellationToken);
    public Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? take = null,
        params string[] includePaths)
    {
        var query = Entities.AsQueryable();
        if (filter != null)
            query = query.Where(filter);
        if (orderBy != null)
            query = orderBy(query);
        if (skip.HasValue)
            query = query.Skip(skip.Value);
        if (take.HasValue)
            query = query.Take(take.Value);
        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<TEntity>> GetWithIncludesAsync(Expression<Func<TEntity, bool>>? predicate = null, params string[] includePaths)
    {
        var query = Entities.AsQueryable();
        // В фейке мы игнорируем includes, так как данные уже загружены.
        if (predicate != null)
            query = query.Where(predicate);
        return Task.FromResult(query.AsEnumerable());
    }
    #endregion

    #region Оптимизация запросов

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Entities.AsQueryable().FirstOrDefault(predicate.Compile()));
    public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Entities.AsQueryable().SingleOrDefault(predicate.Compile()));
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return Task.FromResult(Entities.Any());
        return Task.FromResult(Entities.Any(predicate.Compile()));
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return Task.FromResult(Entities.Count());
        return Task.FromResult(Entities.Count(predicate.Compile()));
    }

    #endregion

    public Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder, CancellationToken cancellationToken = default)
    {
        var query = queryBuilder(Entities.AsQueryable());
        return Task.FromResult(query.ToList().AsEnumerable());
    }

    #endregion
}