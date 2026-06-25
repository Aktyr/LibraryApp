namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<LibraryContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var provider = configuration["Database:Provider"] ?? "Postgres";
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string not found.");

            options.UseLazyLoadingProxies();

            switch (provider.ToLower())
            {
                case "postgres":
                    options.UseNpgsql(connectionString);
                    break;
                /*case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;*/
                default:
                    throw new NotSupportedException($"Provider '{provider}' is not supported.");
            }
        });
        return services;
    }

}