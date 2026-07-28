namespace LibApp.ApplicationTests;

internal class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    public List<TEntity> Entities { get; init; } = [];

    #region IRepository

    #region CRUD
    public Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            Entities.AddRange(entities);
        }, cancellationToken);

    public Task<IEnumerable<TEntity>> Get(CancellationToken cancellationToken = default) =>
        Task.FromResult(Entities.AsEnumerable());

    public Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var func = predicate.Compile();
        return Task.FromResult(Entities.Where(func).AsEnumerable());
    }


    public Task<IEnumerable<TEntity>> GetWithoutTracking(CancellationToken cancellationToken = default) =>
        Get(cancellationToken);
    public Task<IEnumerable<TEntity>> GetWithoutTracking(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Get(predicate, cancellationToken);

    public Task RemoveRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.Run(() => Entities.RemoveAll(x => entities.Contains(x)));

    public Task UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
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
    #endregion

    public IQueryable<TEntity> GetQueryable() => Entities.AsQueryable();
    public IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate) => Entities.Where(predicate.Compile()).AsQueryable();

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
}