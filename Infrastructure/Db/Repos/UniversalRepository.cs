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
    public async Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> Get(CancellationToken cancellationToken)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> Get(Id id, CancellationToken cancellationToken)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id.Value == id.Value, cancellationToken);
    }
    public async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);


    public async Task<IEnumerable<TEntity>> GetWithoutTracking(CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetWithoutTracking(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public IQueryable<TEntity> GetQueryable() => _dbSet;
    public IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate) => _dbSet.Where(predicate);

    #endregion
}
