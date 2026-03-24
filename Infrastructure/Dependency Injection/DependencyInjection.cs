namespace LibApp.Infrastructure.DependencyInjection;

// AddTransient Новый экземпляр каждый раз        При каждом запросе  После использования
// AddScoped    Один экземпляр на HTTP - запрос   В начале запроса В конце запроса
// AddSingleton Один экземпляр на всё приложение  При первом запросе При завершении приложения

public static class DependencyInjection
{
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
        services.AddTransient<IConverter<Book, BookDTO>, BookDTOConverter>();
        services.AddTransient<IConverter<Room, RoomDTO>, RoomDTOConverter>();
        services.AddTransient<IConverter<RoomBook, RoomBookDTO>, RoomBookDTOConverter>();
        services.AddTransient<IConverter<User, UserDTO>, UserDTOConverter>();
        services.AddTransient<IConverter<UserRoomBook, UserRoomBookDTO>, UserRoomBookDTOConverter>();
        services.AddTransient<IConverter<UserRoomBook, BorrowedBookDTO>, BorrowDTOConverter>();

        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddTransient<BookValidatorAsync>();
        services.AddTransient<RoomValidatorAsync>();
        services.AddTransient<RoomBookValidatorAsync>();
        services.AddTransient<UserValidatorAsync>();
        services.AddTransient<RegisterValidatorAsync>();
        services.AddTransient<BorrowingValidatorAsync>();
        return services;
    }

    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.AddTransient<LoginCommand>();
        services.AddTransient<RegisterCommand>();
        services.AddTransient<CreateBookCommand>();
        services.AddTransient<DeleteBookCommand>();
        services.AddTransient<GetAllBooksCommand>();
        services.AddTransient<GetBookCommand>();
        services.AddTransient<UpdateBookCommand>();
        services.AddTransient<CreateRoomCommand>();
        services.AddTransient<DeleteRoomCommand>();
        services.AddTransient<GetAllRoomsCommand>();
        services.AddTransient<GetRoomCommand>();
        services.AddTransient<UpdateRoomCommand>();
        services.AddTransient<CreateUserCommand>();
        services.AddTransient<DeleteUserCommand>();
        services.AddTransient<GetAllUsersQuery>();
        services.AddTransient<GetUserQuery>();
        services.AddTransient<UpdateUserCommand>();
        services.AddTransient<BorrowBookCommand>();
        services.AddTransient<ReturnBookCommand>();
        services.AddTransient<ExtendDeadlineCommand>();
        services.AddTransient<GetUserRoomBooksQuery>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "LibraryApp";
        var audience = jwtSettings["Audience"] ?? "LibraryApp";

        services.AddSingleton(new JwtService(secretKey, issuer, audience));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

        return services;
    }
}
