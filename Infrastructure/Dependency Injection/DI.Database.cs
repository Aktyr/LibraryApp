namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<LibraryContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json");
            }

            options.UseLazyLoadingProxies();
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}