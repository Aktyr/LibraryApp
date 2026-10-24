namespace BlazorWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        builder.Services.AddDbContext<LibraryContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "library")).UseLazyLoadingProxies());

        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<LibraryContext>());

        // Сервисы создания Library-пользователя
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<UserValidatorAsync>();
        builder.Services.AddScoped<EmailValidatorAsync>();

        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection("ApplicationSettings:Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddScoped<JwtService>();


        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


        //  HttpClient для форварда к WebAPI (с JWT-хендлером) 
        builder.Services.AddTransient<AuthenticatedWebApiHandler>();
        builder.Services.AddHttpClient("webapi", client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7271/");
        }).AddHttpMessageHandler<AuthenticatedWebApiHandler>();

        //  HttpClient для компонентов Blazor.Server (base URI текущего запроса) 
        builder.Services.AddScoped(sp =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var baseUri = ctx is not null
                ? new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/")
                : new Uri("https://localhost:7198/");
            return new HttpClient { BaseAddress = baseUri };
        });
        
        // API-клиенты для компонентов Blazor.Server (через прокси-эндпоинты)
        builder.Services.AddScoped<IBookApiClient, BookApiClient>();
        builder.Services.AddScoped<IRoomApiClient, RoomApiClient>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

        // API-прокси для компонентов Blazor.Server (через HttpClient с base URI текущего запроса)
        app.MapBooksProxy();
        app.MapRoomsProxy();

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}
