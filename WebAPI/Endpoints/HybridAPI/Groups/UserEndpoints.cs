namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Users")]
[Route("/api/users")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class UserEndpoints : EndpointBase
{
    [HttpGet]
    public async Task<IResult> GetAll(GetAllUsersCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new EmptyRequest(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(Guid id, GetUserCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new GetUserRequest { Id = new Id(id) }, ct));

    [HttpGet("{id:guid}/books")]
    public async Task<IResult> GetUserBooks(Guid id, GetUserRoomBooksCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new GetUserBooksRequest { UserId = id }, ct));

    [HttpPost]
    public async Task<IResult> Create(
        CreateUserCommand command,
        CreateUserRequest request,
        HttpContext context,
        CancellationToken ct)
    {
        var actorRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

        // Только Admin может создавать Librarian/Admin.
        if (actorRole != nameof(UserRole.Admin)
            && request.Role.HasValue
            && request.Role.Value != UserRole.Reader)
        {
            return Results.Problem(
                detail: "Только администратор может создавать пользователей с ролью Библиотекарь или Администратор",
                statusCode: 403);
        }

        return Results.Ok(await command.Execute(request, ct));
    }

    [HttpPut]
    public async Task<IResult> Update(
        UpdateUserCommand command,
        UpdateUserRequest request,
        GetUserCommand getUser,
        HttpContext context,
        CancellationToken ct)
    {
        var actorRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

        // Не админ редактирует только Reader.
        if (actorRole != nameof(UserRole.Admin))
        {
            var existing = await getUser.Execute(new GetUserRequest { Id = request.Id }, ct);
            var target = existing?.Users.FirstOrDefault();
            if (target is null) throw new UserNotFoundException();

            if (target.Role != UserRole.Reader)
            {
                return Results.Problem(
                    detail: "Только администратор может редактировать Библиотекарей и Администраторов",
                    statusCode: 403);
            }
        }

        return Results.Ok(await command.Execute(request, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(
        Guid id,
        DeleteUserCommand command,
        HttpContext context,
        CancellationToken ct)
    {
        var actorRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

        // Только админ может удалять пользователей.
        if (actorRole != nameof(UserRole.Admin))
        {
            return Results.Problem(
                detail: "Только администратор может удалять пользователей",
                statusCode: 403);
        }

        return Results.Ok(await command.Execute(new DeleteUserRequest { Id = new Id(id) }, ct));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IResult> ChangeStatus(
    Guid id,
    ChangeSubscriptionStatusRequest request,
    ChangeSubscriptionStatusCommand command,
    CancellationToken ct)
    {
        request.UserId = new Id(id); // защита от подмены ID в body
        return Results.Ok(await command.Execute(request, ct));
    }

    [HttpGet("my-profile")]
    [EnumAuthorize(UserRole.Reader, UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> GetMe(GetUserCommand command, ICurrentUserService currentUser, CancellationToken ct)
    {
        if (currentUser.LibraryUserId == null) return Results.Unauthorized();
        return Results.Ok(await command.Execute(new GetUserRequest { Id = currentUser.LibraryUserId }, ct));
    }

    [HttpGet("my-profile/books")]
    [EnumAuthorize(UserRole.Reader, UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> GetMyBooks(GetUserRoomBooksCommand command, ICurrentUserService currentUser, CancellationToken ct)
    {
        if (currentUser.LibraryUserId == null) return Results.Unauthorized();
        return Results.Ok(await command.Execute(new GetUserBooksRequest { UserId = currentUser.LibraryUserId.Value }, ct));
    }
}