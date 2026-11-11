namespace BlazorWeb;

public class Program
{
    /// <summary>
    /// Точка входа приложения. Настраивает DI, Identity, HTTP-пайплайн и запускает фоновые сервисы.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        #region Регистрация компонентов и сервисов

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        // Вызов кастомных расширений для разгрузки Program.cs
        builder.Services.AddBlazorWebInfrastructure(builder.Configuration);
        builder.Services.AddBlazorWebApplicationLogic();
        builder.Services.AddBlazorWebHttpClients(builder.Configuration);

        #endregion

        #region Настройки Identity и JWT

        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection("ApplicationSettings:Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;

            // Правила пароля — в PasswordPolicy (Core/Policies). Здесь — Identity-опции,
            // которые читают встроенные валидаторы (сейчас заменены на UnicodePasswordValidator).
            // Значения синхронизированы с PasswordPolicy; RequiredUniqueChars оставлен для совместимости.
            options.Password.RequiredLength = PasswordPolicy.MinLength;
            options.Password.RequiredUniqueChars = 1;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Заменяем стандартный PasswordValidator на Unicode-aware.
        // Это обходит баг .NET 10, где стандартный валидатор ложно отклоняет кириллические пароли
        // с ошибкой "Passwords must have at least one lowercase ('a'-'z')".
        builder.Services.RemoveAll<IPasswordValidator<ApplicationUser>>();
        builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, UnicodePasswordValidator>();

        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppUserClaimsPrincipalFactory>();
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        #endregion

        var app = builder.Build();

        #region Конфигурация HTTP-пайплайна

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode()
           .AddInteractiveWebAssemblyRenderMode()
           .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        #endregion

        #region Эндпоинты API и Identity

        // Прокси-эндпоинты для работы клиентских компонентов через BFF
        app.MapBooksProxy();
        app.MapRoomsProxy();
        app.MapUsersProxy();
        app.MapBorrowingProxy();
        app.MapReportsProxy();
        app.MapSettingsProxy();
        app.MapAuditProxy();
        app.MapAuthProxy();

        // Дополнительные эндпоинты Identity (Logout, Manage, ExternalLogin и т.д.)
        app.MapAdditionalIdentityEndpoints();

        #endregion

        #region Инициализация данных (Seed)

        // Identity-миграции применяются только в Development (docker-compose).
        // В production — отдельный job/migration-runner, не стартап приложения.
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            identityDb.Database.Migrate();
        }

        // Выполняется при старте, чтобы гарантировать наличие администратора в системе
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            try
            {
                await AdminSeeder.SeedAsync(app.Services, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Admin seed failed. App will continue without seed.");
            }
        }

        #endregion

        app.Run();
    }
}