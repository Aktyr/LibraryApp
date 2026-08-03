namespace WebAPI.Endpoints.BaseAPI.Groups;

[Route("api/users")]
[EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
public class UserController : BaseApiController
{
    public UserController(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> GetAll(CancellationToken cancellationToken)
    {
        var result = await ExecuteQuery<GetAllUsersCommand, EmptyRequest, UserResponse>(
            new EmptyRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetUserRequest { Id = new Id(id) };
        var result = await ExecuteQuery<GetUserCommand, GetUserRequest, UserResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/books")]
    public async Task<ActionResult<BorrowResponse>> GetUserBooks(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetUserBooksRequest { UserId = id };
        var result = await ExecuteQuery<GetUserRoomBooksCommand, GetUserBooksRequest, BorrowResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<CreateUserCommand, CreateUserRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Update(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<UpdateUserCommand, UpdateUserRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeleteUserRequest { Id = new Id(id) };
        var result = await ExecuteCommand<DeleteUserCommand, DeleteUserRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }
}