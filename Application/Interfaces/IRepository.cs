namespace LibApp.Application.Interfaces;

public interface IRepository<TEntity> where TEntity : class, IEntity
{
    #region CRUD
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

    // Рекурсия
    //Task Get(Id id, CancellationToken cancellationToken) =>
    //     Get(id, cancellationToken); 

    Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTracking(CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTracking(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
    #endregion

    // Для поиска в SQL
    IQueryable<TEntity> GetQueryable();
    IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate);

    // Получить сущности с указанными Include
    Task<IEnumerable<TEntity>> GetWithIncludesAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params string[] includePaths);
    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? take = null,
        params string[] includePaths);

}