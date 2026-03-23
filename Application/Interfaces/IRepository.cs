using System.Linq.Expressions;

namespace LibApp.Application.Interfaces;

public interface IRepository<TEntity> where TEntity : class, IEntity
{
    Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task Add(TEntity entity, CancellationToken cancellationToken) =>
         AddRange([entity], cancellationToken);

    Task UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task Update(TEntity entity, CancellationToken cancellationToken) =>
         UpdateRange([entity], cancellationToken);
    Task RemoveRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task Remove(TEntity entity, CancellationToken cancellationToken) =>
         RemoveRange([entity], cancellationToken);

    Task<IEnumerable<TEntity>> Get(CancellationToken cancellationToken);
    Task Get(Id id, CancellationToken cancellationToken) =>
         Get(id, cancellationToken); // todo не будет ли рекурсии?

    //Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> predicate, 
    //    CancellationToken cancellationToken); // Заменить функцию ниже на это?

    [Obsolete]
    Task<IEnumerable<TEntity>> Get(Func<TEntity, bool> predicate, CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTracking(CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTracking(Func<TEntity, bool> predicate, CancellationToken cancellationToken);
}