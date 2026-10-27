namespace BlazorWeb.Api;

public static class UsersProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapUsersProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/users")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/{id:guid}/books", GetUserBooksAsync);

        // Переопределяем Create и Delete
        group.MapPost("/", CreateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        group.MapPut("/", UpdateAsync);
    }

    #region Pass-through (WebAPI)

    private static Task<IResult> GetAllAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, "/api/users", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}", null, ct);

    private static Task<IResult> GetUserBooksAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}/books", null, ct);

    private static Task<IResult> UpdateAsync(
        UpdateUserRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Put, "/api/users", request, ct);

    private static async Task<IResult> ForwardAsync(
        IHttpClientFactory httpFactory,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        using var req = new HttpRequestMessage(method, path);
        if (body is not null)
            req.Content = JsonContent.Create(body);

        using var resp = await http.SendAsync(req, ct);
        var content = await resp.Content.ReadAsStringAsync(ct);
        var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
        return Results.Content(content, mediaType, statusCode: (int)resp.StatusCode);
    }

    #endregion

    #region BFF (Identity + Library)

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        UserManager<ApplicationUser> userManager,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("CreateUserBff");

        // Создаём Identity-пользователя
        var appUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true, // Админ создаёт пользователя, подтверждение не нужно
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName ?? "",
            ContactInfo = request.ContactInfo
        };

        var identityResult = await userManager.CreateAsync(appUser, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return Results.Problem(detail: errors, statusCode: 400);
        }

        try
        {
            // Создаём Library-пользователя (вызывает валидацию Email и пароля)
            var libUser = await userService.CreateUserAsync(
                request.Email,
                request.Password,
                request.LastName,
                request.FirstName,
                request.MiddleName ?? "",
                request.ContactInfo,
                request.Role ?? UserRole.Reader,
                ct);

            // Связываем их
            appUser.LibraryUserId = libUser.Id.Value;
            await userManager.UpdateAsync(appUser);

            return Results.Ok(new BasicCreateDeleteResponse("Ok", "User created successfully."));
        }
        catch (Exception ex)
        {
            // Компенсация: если Library упал, откатываем Identity
            logger.LogError(ex, "Failed to create library user for {Email}. Rolling back Identity.", request.Email);
            await userManager.DeleteAsync(appUser);

            // Извлекаем чистое сообщение, если это LibValidationException
            var message = ex is LibValidationException lve ? string.Join("\n", lve.ExceptionDetails) : ex.Message;
            return Results.Problem(detail: message, statusCode: 400);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("DeleteUserBff");

        // Ищем Identity-пользователя по LibraryUserId
        var appUser = userManager.Users.FirstOrDefault(u => u.LibraryUserId == id);

        if (appUser != null)
        {
            var identityResult = await userManager.DeleteAsync(appUser);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                return Results.Problem(detail: $"Identity delete error: {errors}", statusCode: 400);
            }
        }

        // Удаляем Library-пользователя напрямую через UnitOfWork
        // (Чтобы не делать лишний запрос в WebAPI)
        try
        {
            var libUserId = new Id(id);
            var userRepo = unitOfWork.GetRepository<LibApp.Core.Entities.User>();
            var libUser = await userRepo.FirstOrDefaultAsync(u => u.Id == libUserId, ct);

            if (libUser != null)
            {
                await userRepo.RemoveAsync(libUser, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            // Если удаление Library упало (например, FK constraint), 
            // мы уже удалили Identity. Пишем в лог, но возвращаем 200.
            logger.LogError(ex, "Failed to delete library user {Id} after Identity deletion.", id);
            return Results.Problem(detail: $"Пользователь удалён из Identity, но не удалён из библиотеки: {ex.Message}", statusCode: 500);
        }

        return Results.Ok(new BasicCreateDeleteResponse("Ok", "User deleted successfully."));
    }

    #endregion
}