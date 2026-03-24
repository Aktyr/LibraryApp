namespace LibApp.Infrastructure.Db.DbAsFiles;

public class LibraryDataContext<T> : IRepository<T> where T : class, IEntity, new()
{
    private static readonly Lazy<LibraryDataContext<T>> _instance = new(() => new LibraryDataContext<T>());
    public static LibraryDataContext<T> Instance => _instance.Value;

    private readonly FileInteractor<T> _interactor;

    private LibraryDataContext()
    {
        _interactor = InteractorFactory.Create<T>();
    }

    public async Task AddRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            // Если сущность уже существует, удаляем её
            var existing = _interactor.Data.FirstOrDefault(e => e.Id.Value == entity.Id.Value);
            if (existing != null)
            {
                _interactor.Data.Remove(existing);
            }
            _interactor.Data.Add(entity);
        }

        _interactor.SaveChanges();
        await Task.CompletedTask;
    }

    public async Task UpdateRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        // Для файлового хранилища Update = AddOrUpdate
        await AddRange(entities, cancellationToken);
    }

    public async Task RemoveRange(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            _interactor.Data.Remove(entity);
        }

        _interactor.SaveChanges();
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<T>> Get(CancellationToken cancellationToken)
    {
        return await Task.FromResult(_interactor.Data.AsEnumerable());
    }

    public async Task<T?> Get(Id id, CancellationToken cancellationToken)
    {
        return await Task.FromResult(_interactor.Data.FirstOrDefault(e => e.Id.Value == id.Value));
    }

    public async Task<IEnumerable<T>> Get(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        return await Task.FromResult(_interactor.Data.Where(predicate));
    }

    public async Task<IEnumerable<T>> GetWithoutTracking(CancellationToken cancellationToken)
    {
        // Для файлового хранилища нет отслеживания, поэтому возвращаем как есть
        return await Task.FromResult(_interactor.Data.AsEnumerable());
    }

    public async Task<IEnumerable<T>> GetWithoutTracking(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        // Для файлового хранилища нет отслеживания
        return await Task.FromResult(_interactor.Data.Where(predicate));
    }
}
