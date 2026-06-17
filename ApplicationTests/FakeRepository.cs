namespace LibApp.ApplicationTests;

internal class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    public List<TEntity> Entities { get; init; } = [];

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
}