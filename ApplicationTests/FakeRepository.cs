using LibApp.Core.Interfaces;

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

    public Task<IEnumerable<TEntity>> Get(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(Entities.Where(predicate).AsEnumerable());

    public Task<IEnumerable<TEntity>> GetWithoutTracking(CancellationToken cancellationToken = default) =>
        Get(cancellationToken);
    public Task<IEnumerable<TEntity>> GetWithoutTracking(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default) =>
        Get(predicate, cancellationToken);

    public Task RemoveRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.Run(() => Entities.RemoveAll(x => entities.Contains(x)));

    public Task UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
