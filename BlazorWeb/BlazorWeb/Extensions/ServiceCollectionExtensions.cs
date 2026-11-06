namespace BlazorWeb.Extensions;

public static class ServiceCollectionExtensions
{
    // Identity и БД (вручную, так как это специфика Blazor Server)
    public static IServiceCollection AddBlazorWebInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
        services.AddHttpContextAccessor();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddDbContext<LibraryContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "library"))
                   .UseLazyLoadingProxies());

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<LibraryContext>());

        return services;
    }

    /// <summary>
    /// Регистрирует бизнес-логику, необходимую BFF для работы с Identity и пользователями.
    /// Не использует автоматическое сканирование сборки (AddApplicationLogic),
    /// так как оно подтягивает команды и сервисы WebAPI (SMTP, списание книг),
    /// которые в BFF не имеют зависимостей и приводят к ошибке валидации DI.
    /// </summary>
    public static IServiceCollection AddBlazorWebApplicationLogic(this IServiceCollection services)
    {
        services.AddRepositories();
        services.AddUnitOfWork();

        services.AddScoped<IAuditService, BffAuditService>();

        // Валидаторы, используемые UserService и командой удаления
        services.AddScoped<UserValidatorAsync>();

        // Сервисы, напрямую используемые BFF
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<JwtService>();

        // Единственная команда, которую BFF вызывает напрямую (при удалении пользователя)
        services.AddTransient<DeleteUserCommand>();

        // Политика прав и сервис создания пары Identity + Library
        services.AddScoped<IUserManagementPolicy, UserManagementPolicy>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        // Самостоятельное восстановление: снимает soft-delete в library и пересоздаёт Identity.
        services.AddScoped<ISelfRestoreService, SelfRestoreService>();

        // ICurrentUserService для BFF: резолвит library-пользователя из Identity-контекста.
        // Infrastructure.CurrentUserService здесь не подходит — он читает JWT-claims,
        // а в BFF NameIdentifier — это Identity-id, а не library-id.
        services.AddScoped<ICurrentUserService, BffCurrentUserService>();

        return services;
    }

    // HTTP и прокси-клиенты
    public static IServiceCollection AddBlazorWebHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<AuthenticatedWebApiHandler>();

        services.AddHttpClient("webapi", client =>
        {
            client.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7271/");
        }).AddHttpMessageHandler<AuthenticatedWebApiHandler>();

        services.AddScoped(sp =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var baseUri = ctx is not null
                ? new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/")
                : new Uri("https://localhost:7198/");
            return new HttpClient { BaseAddress = baseUri };
        });

        services.AddScoped<IBookApiClient, BookApiClient>();
        services.AddScoped<IRoomApiClient, RoomApiClient>();

        return services;
    }
}