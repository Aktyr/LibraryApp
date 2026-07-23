using LibApp.Infrastructure.Db;

namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(UniversalRepository<>));
        return services;
    }

    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        services.RegisterImplementationsOfOpenGeneric(typeof(IConverter<,>), ServiceLifetime.Singleton);
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
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        var serviceTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IService).IsAssignableFrom(t))
            .Where(t => !t.IsSubclassOf(typeof(BackgroundService))); // исключаем фоновые службы

        foreach (var type in serviceTypes)
        {
            var interfaces = type.GetInterfaces().Where(i => !typeof(IMarker).IsAssignableFrom(i)).ToList();
            if (interfaces.Any())
                foreach (var @interface in interfaces)
                    services.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Scoped));
            else
                services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Scoped));
        }
        return services;
    }
}
