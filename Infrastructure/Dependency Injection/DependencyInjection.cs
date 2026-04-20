namespace LibApp.Infrastructure.DependencyInjection;

// AddTransient Новый экземпляр каждый раз        При каждом запросе  После использования
// AddScoped    Один экземпляр на HTTP - запрос   В начале запроса В конце запроса
// AddSingleton Один экземпляр на всё приложение  При первом запросе При завершении приложения

public static class DependencyInjection
{
    private static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfigurationSettings(configuration);
        services
            .AddDatabase(configuration)
            .AddRepositories()
            .AddConverters()
            .AddValidators()
            .AddCommands()
            .AddServices()
            .AddJwtAuthentication(configuration)
            .AddBackgroundServices();

        return services;
    }
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем настройки для IOptions
        services.Configure<DeadlineCheckSettings>(configuration.GetSection("DeadlineCheck"));
        services.Configure<BorrowingSettings>(configuration.GetSection("Borrowing"));
        services.Configure<PenaltySettings>(configuration.GetSection("Penalty"));
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("Jwt"))
                                          .ValidateDataAnnotations()  // Использует атрибуты [Required], [MinLength] из класса JwtSettings
                                          .ValidateOnStart();         // Приложение упадет сразу, а не при попытке входа
        return services;
    }
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext должен быть Scoped (один на запрос)
        services.AddDbContext<LibraryContext>(options =>
        {
            options.UseLazyLoadingProxies();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(PostgreRepository<>));
        return services;
    }

    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        var converterTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConverter<,>)))
            .ToList();

        foreach (var converterType in converterTypes)
        {
            var interfaceType = converterType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConverter<,>));

            services.AddTransient(interfaceType, converterType);
        }

        return services;
    }
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        var validatorTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IValidator).IsAssignableFrom(t))
            .ToList();

        foreach (var validatorType in validatorTypes)        
            services.AddTransient(validatorType);
        

        return services;
    }
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t))
            .ToList();

        foreach (var commandType in commandTypes)        
            services.AddTransient(commandType);
        

        return services;
    }
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        var serviceTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IService).IsAssignableFrom(t))
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i != typeof(IService))
                .ToList();

            if (interfaces.Any())
            {
                foreach (var interfaceType in interfaces)                
                    services.AddScoped(interfaceType, serviceType);                
            }
            else            
                services.AddScoped(serviceType);            
        }
        return services;
    }
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        var backgroundServiceTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.IsSubclassOf(typeof(BackgroundService)))
            .ToList();

        foreach (var serviceType in backgroundServiceTypes)
        {
            var method = typeof(ServiceCollectionHostedServiceExtensions)
                .GetMethods()
                .First(m => m.Name == nameof(ServiceCollectionHostedServiceExtensions.AddHostedService)
                            && m.IsGenericMethod
                            && m.GetParameters().Length == 1
                            && m.GetParameters()[0].ParameterType == typeof(IServiceCollection));

            method.MakeGenericMethod(serviceType).Invoke(null, new object[] { services });
        }
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT Settings not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });

        return services;
    }
}
