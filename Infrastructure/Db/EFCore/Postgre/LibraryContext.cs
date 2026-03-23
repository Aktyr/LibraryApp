namespace LibApp.Infrastructure.Db.EFCore.Postgre;
// User ID=root;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;
internal class LibraryContext : DbContext
{ 
    private static readonly List<Type> _entityTypes = new()
    {
        typeof(User),
        typeof(Book),
        typeof(Room),
        typeof(RoomBook),
        typeof(UserRoomBook)
    };
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
    public DbSet<T> GetDbSet<T>() where T : class, IEntity => Set<T>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Регистрируем все сущности
        foreach (var entityType in _entityTypes)
            modelBuilder.Entity(entityType);

        ConfigureIdConversion(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureIndexes(modelBuilder);
    }
    private void ConfigureIdConversion(ModelBuilder modelBuilder)
    {
        var idConverter = new ValueConverter<Id, Guid>(
            id => id.Value,
            guid => new Id(guid));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty != null && idProperty.ClrType == typeof(Id))
            {
                idProperty.SetValueConverter(idConverter);
                entityType.SetPrimaryKey(idProperty);
            }
        }

    }
    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomBook>(entity =>
        {
            entity.HasOne(rb => rb.Room)
                .WithMany(r => r.RoomBooks)
                .HasForeignKey("RoomId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rb => rb.Book)
                .WithMany(b => b.RoomBook)
                .HasForeignKey("BookId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRoomBook>(entity =>
        {
            entity.HasOne(urb => urb.User)
                .WithMany(u => u.RoomBooks)
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(urb => urb.RoomBook)
                .WithMany()
                .HasForeignKey("RoomBookId")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Room>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(b => b.Title);
        modelBuilder.Entity<Book>().HasIndex(b => b.Author);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies() // todo сделать что-то со строкой
                      .UseNpgsql("User ID=root;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;");
        base.OnConfiguring(optionsBuilder);
    }

}
