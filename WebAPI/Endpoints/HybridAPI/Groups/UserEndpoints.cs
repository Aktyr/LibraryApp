namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Users")]
[Route("/api/users")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class UserEndpoints : EndpointBase
{
    [HttpGet]
    public async Task<IResult> GetAll(GetAllUsersQuery command, CancellationToken ct) => Results.Ok(await command.Execute(new EmptyRequest(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(Guid id, GetUserQuery command, CancellationToken ct) => Results.Ok(await command.Execute(new GetUserRequest { Id = new Id(id) }, ct));

    [HttpGet("{id:guid}/books")]
    public async Task<IResult> GetUserBooks(Guid id, GetUserRoomBooksQuery command, CancellationToken ct) => Results.Ok(await command.Execute(new GetUserBooksRequest { UserId = id }, ct));

    [HttpPost]
    public async Task<IResult> Create(CreateUserCommand command, CreateUserRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPut]
    public async Task<IResult> Update(UpdateUserCommand command, UpdateUserRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id, DeleteUserCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new DeleteUserRequest { Id = new Id(id) }, ct));
}