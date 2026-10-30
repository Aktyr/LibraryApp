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
        IUserManagementPolicy policy,
        HttpContext context,
        CancellationToken ct)
    {
        var actor = ParseActorRole(context);
        var target = request.Role ?? UserRole.Reader;

        if (!policy.CanCreate(actor, target))
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
        IUserManagementPolicy policy,
        HttpContext context,
        CancellationToken ct)
    {
        var actor = ParseActorRole(context);

        if (actor != UserRole.Admin)
        {
            var existing = await getUser.Execute(new GetUserRequest { Id = request.Id }, ct);
            var target = existing?.Users.FirstOrDefault()
                ?? throw new UserNotFoundException();

            if (!policy.CanEdit(actor, target.Role))
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
        IUserManagementPolicy policy,
        HttpContext context,
        CancellationToken ct)
    {
        if (!policy.CanDelete(ParseActorRole(context)))
        {
            return Results.Problem(
                detail: "Только администратор может удалять пользователей",
                statusCode: 403);
        }

        return Results.Ok(await command.Execute(new DeleteUserRequest { Id = new Id(id) }, ct));
    }

    private static UserRole ParseActorRole(HttpContext ctx)
    {
        var raw = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(raw, ignoreCase: false, out var r) ? r : UserRole.Reader;
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