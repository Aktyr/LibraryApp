namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<LibraryContext>((serviceProvider, options) =>
        {
            var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseLazyLoadingProxies();
            options.UseNpgsql(dbSettings.ConnectionString);
        });

        return services;
    }
}
