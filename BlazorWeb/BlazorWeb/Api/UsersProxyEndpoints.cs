using LibApp.Core.Interfaces;

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
                request.Email,
                request.Password,
                request.LastName,
                request.FirstName,
                request.MiddleName ?? "",
                request.ContactInfo,
                targetRole,
                emailConfirmed: true,
                ct);

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