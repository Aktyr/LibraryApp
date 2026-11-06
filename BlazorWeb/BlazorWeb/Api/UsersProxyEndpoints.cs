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
        group.MapDelete("/my-profile", DeleteMyProfileAsync);
        group.MapGet("/my-profile", GetMeAsync);
        group.MapGet("/my-profile/books", GetMyBooksAsync);
        group.MapPut("/{id:guid}/status", ChangeStatusAsync);
        group.MapGet("/deleted", GetDeletedAsync);
        group.MapPost("/{id:guid}/restore", RestoreAsync);


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

    private static Task<IResult> GetDeletedAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/users/deleted", null, ct);

    private static Task<IResult> RestoreAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, $"/api/users/{id}/restore", null, ct);

    #endregion

    #region BFF (Identity + Library)

    private static async Task<IResult> CreateAsync(
    CreateUserRequest request,
    ClaimsPrincipal user,
    IUserManagementPolicy policy,
    IUserProvisioningService provisioning,
    ILoggerFactory loggerFactory,
    CancellationToken ct)
    {
        var actorRole = ParseActorRole(user);
        var targetRole = request.Role ?? UserRole.Reader;

        if (!policy.CanCreate(actorRole, targetRole))
        {
            return Results.Problem(
                detail: "Только администратор может создавать пользователей с ролью Библиотекарь или Администратор",
                statusCode: 403);
        }

        try
        {
            await provisioning.CreateAsync(
                email: request.Email,
                password: request.Password,
                lastName: request.LastName,
                firstName: request.FirstName,
                middleName: request.MiddleName ?? "",
                contactInfo: request.ContactInfo,
                role: targetRole,
                emailConfirmed: true,
                isSelfRegistration: false,
                ct: ct);

            return Results.Ok(new BasicCreateDeleteResponse("Ok", "User created successfully."));
        }
        catch (ProvisioningException pex)
        {
            loggerFactory.CreateLogger("CreateUserBff").LogWarning(
                "User provisioning failed: {Kind} — {Errors}",
                pex.Kind, string.Join("; ", pex.Errors));

            return Results.Problem(
                detail: string.Join("\n", pex.Errors),
                statusCode: 400);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        DeleteUserCommand deleteUserCommand,
        IUserManagementPolicy policy,
        IUnitOfWork unitOfWork,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!policy.CanDelete(ParseActorRole(user)))
        {
            return Results.Problem(
                detail: "Только администратор может деактивировать пользователей",
                statusCode: 403);
        }

        var logger = loggerFactory.CreateLogger("DeleteUserBff");

        var libUserId = new Id(id);
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == libUserId && urb.ReturnDate == null, ct);

        if (activeBorrows > 0)
        {
            return Results.Problem(
                detail: $"Нельзя деактивировать пользователя: у него {activeBorrows} активных выдач. Сначала верните все книги.",
                statusCode: 400);
        }

        var appUser = userManager.Users.FirstOrDefault(u => u.LibraryUserId == id);
        if (appUser != null)
        {
            var identityResult = await userManager.DeleteAsync(appUser);
            if (!identityResult.Succeeded)
            {
                return Results.Problem(
                    detail: $"Identity delete error: {string.Join(", ", identityResult.Errors.Select(e => e.Description))}",
                    statusCode: 400);
            }
        }

        try
        {
            await deleteUserCommand.Execute(new DeleteUserRequest { Id = new Id(id) }, ct);
        }
        catch (UserNotFoundException) { /* идемпотентно */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to soft-delete library user {Id} after Identity deletion.", id);
            return Results.Problem(
                detail: $"Пользователь удалён из Identity, но не деактивирован в библиотеке: {ex.Message}",
                statusCode: 500);
        }

        return Results.Ok(new BasicCreateDeleteResponse("Ok", "Пользователь деактивирован"));
    }

    private static async Task<IResult> DeleteMyProfileAsync(
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICurrentUserService currentUser,
        SelfDeleteCommand selfDeleteCommand,
        RestoreUserCommand restoreUserCommand,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("DeleteMyProfileBff");

        if (currentUser.LibraryUserId is null)
            return Results.Unauthorized();

        var appUser = await userManager.GetUserAsync(user);
        if (appUser is null)
            return Results.Unauthorized();

        try
        {
            await selfDeleteCommand.Execute(
                new DeleteUserRequest { Id = currentUser.LibraryUserId }, ct);
        }
        catch (LibValidationException ex)
        {
            return Results.Problem(
                detail: string.Join("\n", ex.ExceptionDetails),
                statusCode: 400);
        }

        // Компенсация: library уже soft-deleted, если Identity-удаление упадёт —
        // возвращаем library обратно, иначе пользователь останется без Identity и
        // с активной library-записью (self-restore не сработает: !IsDeleted).
        var identityResult = await userManager.DeleteAsync(appUser);
        if (!identityResult.Succeeded)
        {
            try
            {
                await restoreUserCommand.Execute(
                    new RestoreUserRequest { Id = currentUser.LibraryUserId }, ct);
                logger.LogWarning(
                    "Self-delete: Identity deletion failed, library user {Id} rolled back",
                    currentUser.LibraryUserId.Value);
            }
            catch (Exception compEx)
            {
                logger.LogCritical(compEx,
                    "Self-delete compensation FAILED: library user {Id} is deleted, Identity still exists",
                    currentUser.LibraryUserId.Value);
            }

            return Results.Problem(
                detail: "Не удалось удалить аккаунт. Попробуйте позже или обратитесь к администратору.",
                statusCode: 500);
        }

        await signInManager.SignOutAsync();

        // Cookie статуса читает StatusMessage.razor на /Account/Login.
        // Дублируем private-настройки IdentityRedirectManager.StatusCookieBuilder —
        // шаблон не трогаем.
        httpContext.Response.Cookies.Append(
            IdentityRedirectManager.StatusCookieName,
            "Аккаунт удалён. Восстановить можно по email и паролю.",
            new CookieOptions
            {
                SameSite = SameSiteMode.Strict,
                HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(5),
            });

        return Results.Ok(new BasicCreateDeleteResponse("Ok", "Аккаунт удалён"));
    }

    private static UserRole ParseActorRole(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(raw, ignoreCase: false, out var r) ? r : UserRole.Reader;
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