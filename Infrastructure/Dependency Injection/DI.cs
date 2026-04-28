namespace LibApp.Infrastructure.DependencyInjection;

// AddTransient Новый экземпляр каждый раз        При каждом запросе  После использования
// AddScoped    Один экземпляр на HTTP - запрос   В начале запроса В конце запроса
// AddSingleton Один экземпляр на всё приложение  При первом запросе При завершении приложения

// DI разбит на partial
public static partial class DI
{
    private static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;

    public static IServiceCollection AddAllServices(this IServiceCollection services)
    {
        services.AddConfigurationSettings()
                .AddDatabase()
                .AddRepositories()
                .AddConverters()
                .AddValidators()
                .AddCommands()
                .AddServices()
                .AddJwtAuthentication()
                .AddBackgroundServices();
        return services;
    }

    #region Helper Methods
    /// <summary>
    /// Универсальный метод регистрации всех типов, реализующих указанный маркерный интерфейс.
    /// Автоматически определяет подходящий интерфейс для регистрации, исключая маркерные.
    /// </summary>
    private static IServiceCollection RegisterMarkedTypes<TMarker>(this IServiceCollection services, ServiceLifetime lifetime) where TMarker : IMarker
    {
        var types = ApplicationAssembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && typeof(TMarker).IsAssignableFrom(t));

        foreach (var type in types)
        {
            var registered = false;

            // Ищем все интерфейсы, кроме маркерных (IMarker и его наследников)
            var interfaces = type.GetInterfaces().Where(i => !typeof(IMarker).IsAssignableFrom(i)).ToList();

            foreach (var @interface in interfaces)
            {
                services.Add(new ServiceDescriptor(@interface, type, lifetime));
                registered = true;
            }

            // Если подходящих интерфейсов нет, регистрируем сам тип
            if (!registered)
                services.Add(new ServiceDescriptor(type, type, lifetime));
        }
        return services;
    }

    /// <summary>
    /// Регистрирует все реализации открытого generic-интерфейса
    /// </summary>
    private static IServiceCollection RegisterImplementationsOfOpenGeneric(this IServiceCollection services, Type openGenericType, ServiceLifetime lifetime)
    {
        var implementations = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericType));

        foreach (var implementation in implementations)
        {
            var @interface = implementation.GetInterfaces().First(i => i.IsGenericType
                                                                    && i.GetGenericTypeDefinition()
                                                                    == openGenericType);

            services.Add(new ServiceDescriptor(@interface, implementation, lifetime));
        }
        return services;
    }

    // Перегрузка принимающая Type
    private static IServiceCollection AddHostedService(this IServiceCollection services, Type serviceType)
    {
        var hostedServiceType = typeof(IHostedService);

        if (!hostedServiceType.IsAssignableFrom(serviceType))
            throw new ArgumentException($"{serviceType.Name} must implement IHostedService");

        services.Add(new ServiceDescriptor(hostedServiceType, serviceType, ServiceLifetime.Singleton));

        return services;
    }
    #endregion
}