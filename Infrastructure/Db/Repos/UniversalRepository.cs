namespace LibApp.Infrastructure.Db.Repos;

public class UniversalRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    public UniversalRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    #region IRepository

    #region CRUD
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        //await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        _dbSet.UpdateRange(entities);
        //await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        _dbSet.RemoveRange(entities);
        //await _context.SaveChangesAsync(cancellationToken);
    }


    //public async Task<TEntity?> Get(Id id, CancellationToken cancellationToken)
    //{
    //    return await _dbSet.FirstOrDefaultAsync(e => e.Id.Value == id.Value, cancellationToken);
    //}
    public async Task<IEnumerable<TEntity>> GetAsync(CancellationToken cancellationToken)
        => await _dbSet.ToListAsync(cancellationToken);
    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    public async Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(CancellationToken cancellationToken)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    public async Task<IEnumerable<TEntity>> GetWithoutTrackingAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? skip = null,
        int? take = null,
        params string[] includePaths)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var path in includePaths)
            query = query.Include(path);

        if (filter != null)
            query = query.Where(filter);

        if (orderBy != null)
            query = orderBy(query);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }
    #endregion

    #region Оптимизация запросов

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    public async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.SingleOrDefaultAsync(predicate, cancellationToken);
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
        ? await _dbSet.AnyAsync(cancellationToken)
        : await _dbSet.AnyAsync(predicate, cancellationToken);
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
        ? await _dbSet.CountAsync(cancellationToken)
        : await _dbSet.CountAsync(predicate, cancellationToken);

    #endregion

    public async Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        var query = queryBuilder(_dbSet);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion
}
