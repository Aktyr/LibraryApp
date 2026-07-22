namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Users")]
[Route("/api/users")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class UserEndpoints : EndpointBase
{
    [HttpGet]
    public IResult GetAll(GetAllUsersQuery command, CancellationToken ct) => Results.Ok(command.Execute(new EmptyRequest(), ct));

    [HttpGet("{id:guid}")]
    public IResult GetById(Guid id, GetUserQuery command, CancellationToken ct) => Results.Ok(command.Execute(new GetUserRequest { Id = new Id(id) }, ct));

    [HttpGet("{id:guid}/books")]
    public IResult GetUserBooks(Guid id, GetUserRoomBooksQuery command, CancellationToken ct) => Results.Ok(command.Execute(new GetUserBooksRequest { UserId = id }, ct));

    [HttpPost]
    public IResult Create(CreateUserCommand command, CreateUserRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPut]
    public IResult Update(UpdateUserCommand command, UpdateUserRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public IResult Delete(Guid id, DeleteUserCommand command, CancellationToken ct) => Results.Ok(command.Execute(new DeleteUserRequest { Id = new Id(id) }, ct));
}