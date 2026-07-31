namespace LibApp.Application.Interfaces;

public interface IRepository<TEntity> where TEntity : class, IEntity
{
    #region CRUD
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken) =>
         AddRangeAsync([entity], cancellationToken);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken) =>
         UpdateRangeAsync([entity], cancellationToken);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task RemoveAsync(TEntity entity, CancellationToken cancellationToken) =>
         RemoveRangeAsync([entity], cancellationToken);

    // Рекурсия
    //Task Get(Id id, CancellationToken cancellationToken) =>
    //     Get(id, cancellationToken); 
    Task<IEnumerable<TEntity>> GetAsync(CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);

    // Получить сущности с указанными Include    
    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? take = null,
        params string[] includePaths);

    #endregion

    // todo заменить неоптимизированные методы на оптимизированные
    #region Оптимизация запросов
    /// <summary>
    /// Возвращает первый элемент, удовлетворяющий условию, или null, если таких нет.
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает единственный элемент, удовлетворяющий условию, или null, если таких нет.
    /// Если условию удовлетворяет более одного элемента, выбрасывается исключение.
    /// </summary>
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, существует ли хотя бы один элемент, удовлетворяющий условию.
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает количество элементов, удовлетворяющих условию.
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    #endregion

    Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken = default);

}