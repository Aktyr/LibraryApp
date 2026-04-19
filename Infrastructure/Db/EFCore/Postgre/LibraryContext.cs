namespace LibApp.Infrastructure.Db.EFCore.Postgre;
// User ID=root;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;
public class LibraryContext : DbContext
{
    private static readonly IReadOnlyList<Type> EntityTypes;
    static LibraryContext()
    {
        var coreAssembly = Assembly.GetAssembly(typeof(IEntity));

        EntityTypes = coreAssembly!.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEntity).IsAssignableFrom(t))
            .ToList()
            .AsReadOnly();
    }
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
    public DbSet<T> GetDbSet<T>() where T : class, IEntity => Set<T>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Регистрируем все сущности
        foreach (var entityType in EntityTypes)
            modelBuilder.Entity(entityType);

        ConfigureIdConversion(modelBuilder);
        ConfigureIndexes(modelBuilder);
    }
    private void ConfigureIdConversion(ModelBuilder modelBuilder)
    {
        var idConverter = new ValueConverter<Id, Guid>(
            id => id.Value,
            guid => new Id(guid));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty(nameof(IEntity.Id));
            if (idProperty != null && idProperty.ClrType == typeof(Id))
            {
                idProperty.SetValueConverter(idConverter);
                entityType.SetPrimaryKey(idProperty);
            }
        }

    }
    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Room>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(b => b.Title);
        modelBuilder.Entity<Book>().HasIndex(b => b.Author);
    }
}
