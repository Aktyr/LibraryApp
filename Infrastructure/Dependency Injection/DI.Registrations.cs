namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(PostgreRepository<>));
        return services;
    }

    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        services.RegisterImplementationsOfOpenGeneric(typeof(IConverter<,>), ServiceLifetime.Transient);
        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.RegisterMarkedTypes<IValidator>(ServiceLifetime.Transient);
        return services;
    }

    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.RegisterMarkedTypes<ICommand>(ServiceLifetime.Transient);
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.RegisterMarkedTypes<IService>(ServiceLifetime.Scoped);
        return services;
    }
}
