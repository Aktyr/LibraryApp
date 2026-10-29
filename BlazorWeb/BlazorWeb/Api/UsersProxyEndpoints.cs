namespace BlazorWeb.Api;

public static class UsersProxyEndpoints
{
    public static void MapUsersProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/users")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/{id:guid}/books", GetUserBooksAsync);
        group.MapGet("/my-profile", GetMeAsync);
        group.MapGet("/my-profile/books", GetMyBooksAsync);
        group.MapPut("/{id:guid}/status", ChangeStatusAsync);

        // Переопределяем Create и Delete (BFF: Identity + Library, с компенсацией)
        group.MapPost("/", CreateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapPut("/", UpdateAsync);
    }

    #region Pass-through (WebAPI)

    private static Task<IResult> GetAllAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/users", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}", null, ct);

    private static Task<IResult> GetUserBooksAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}/books", null, ct);

    private static Task<IResult> UpdateAsync(
        UpdateUserRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Put, "/api/users", request, ct);

    #endregion

    #region BFF (Identity + Library)

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        // Только Admin может создавать Librarian/Admin.
        var actorRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (actorRole != nameof(UserRole.Admin)
            && request.Role.HasValue
            && request.Role.Value != UserRole.Reader)
        {
            return Results.Problem(
                detail: "Только администратор может создавать пользователей с ролью Библиотекарь или Администратор",
                statusCode: 403);
        }

        var logger = loggerFactory.CreateLogger("CreateUserBff");

        var appUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
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
            var libUser = await userService.CreateUserAsync(
                request.Email,
                request.Password,
                request.LastName,
                request.FirstName,
                request.MiddleName ?? "",
                request.ContactInfo,
                request.Role ?? UserRole.Reader,
                ct);

            appUser.LibraryUserId = libUser.Id.Value;
            await userManager.UpdateAsync(appUser);

            return Results.Ok(new BasicCreateDeleteResponse("Ok", "User created successfully."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create library user for {Email}. Rolling back Identity.", request.Email);
            await userManager.DeleteAsync(appUser);

            var message = ex is LibValidationException lve
                ? string.Join("\n", lve.ExceptionDetails)
                : ex.Message;
            return Results.Problem(detail: message, statusCode: 400);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        DeleteUserCommand deleteUserCommand,
        IUnitOfWork unitOfWork,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var actorRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (actorRole != nameof(UserRole.Admin))
        {
            return Results.Problem(
                detail: "Только администратор может деактивировать пользователей",
                statusCode: 403);
        }

        var logger = loggerFactory.CreateLogger("DeleteUserBff");

        // Валидация ДО удаления Identity.
        // Если есть активные выдачи — отказываем сразу, не трогая Identity,
        // чтобы не оставить «осиротевший» Identity-аккаунт.
        var libUserId = new Id(id);
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == libUserId && urb.ReturnDate == null,
            ct);

        if (activeBorrows > 0)
        {
            return Results.Problem(
                detail: $"Нельзя деактивировать пользователя: у него {activeBorrows} активных выдач. Сначала верните все книги.",
                statusCode: 400);
        }

        // Удаляем Identity-пользователя (жёстко — Identity-запись
        // не нужна, её историю не храним; library.User для истории остаётся).
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

        // Soft-delete library.User через команду — единая точка правды
        // для правил деактивации. DeleteUserCommand ещё раз проверит
        // activeBorrows (защита от race), но это уже перестраховка.
        try
        {
            await deleteUserCommand.Execute(new DeleteUserRequest { Id = new Id(id) }, ct);
        }
        catch (UserNotFoundException)
        {
            // library.User нет — например, был удалён раньше.
            // Identity-сироту мы уже удалили — считаем успехом (идемпотентно).
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to soft-delete library user {Id} after Identity deletion.", id);
            return Results.Problem(
                detail: $"Пользователь удалён из Identity, но не деактивирован в библиотеке: {ex.Message}",
                statusCode: 500);
        }

        return Results.Ok(new BasicCreateDeleteResponse("Ok", "Пользователь деактивирован"));
    }
    private static Task<IResult> ChangeStatusAsync(
        Guid id, ChangeSubscriptionStatusRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Put, $"/api/users/{id}/status", request, ct);
    private static Task<IResult> GetMeAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/users/my-profile", null, ct);

    private static Task<IResult> GetMyBooksAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/users/my-profile/books", null, ct);

    #endregion
}