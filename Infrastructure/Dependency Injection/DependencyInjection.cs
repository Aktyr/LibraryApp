namespace LibApp.Infrastructure.DependencyInjection;

// AddTransient Новый экземпляр каждый раз        При каждом запросе  После использования
// AddScoped    Один экземпляр на HTTP - запрос   В начале запроса В конце запроса
// AddSingleton Один экземпляр на всё приложение  При первом запросе При завершении приложения

// todo DI выглядит очень нагруженным, следует разбить на partial
public static partial class DependencyInjection
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

    #region Settings Registration
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services)
    {
        // Регистрируем все настройки с валидацией при старте
        var settingsTypes = typeof(ISettings).Assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISettings).IsAssignableFrom(t));

        foreach (var type in settingsTypes)
        {
            typeof(DependencyInjection).GetMethod(nameof(ConfigureAndValidate), BindingFlags.NonPublic | BindingFlags.Static)!
                                       .MakeGenericMethod(type)
                                       .Invoke(null, [services]);
        }
        return services;
    }
    private static IServiceCollection ConfigureAndValidate<T>(this IServiceCollection services) where T : class
    {
        // Вырезаем "Settings" для лучшего нейминга
        var sectionName = typeof(T).Name.Replace("Settings", "");

        return services.AddOptions<T>()
                       .BindConfiguration(sectionName)
                       .ValidateDataAnnotations()
                       .ValidateOnStart()
                       .Services;
    }
    #endregion

    #region Database
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
    #endregion

    #region Auto-Registration
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
    #endregion

    #region Authentication
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Получаем IOptions<JwtSettings> и настраиваем напрямую
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtSettings) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings.Value.Issuer,
                    ValidAudience = jwtSettings.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Value.SecretKey))
                };
            });

        return services;
    }

    #endregion

    #region Background Services
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        var backgroundServiceTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && t.IsSubclassOf(typeof(BackgroundService)));

        foreach (var serviceType in backgroundServiceTypes)
        {
            services.AddTransient(serviceType);
            services.AddHostedService(serviceType);
        }
        return services;
    }
    #endregion

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