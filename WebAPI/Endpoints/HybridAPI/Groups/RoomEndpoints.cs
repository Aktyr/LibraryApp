namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Rooms")]
[Route("/api/rooms")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class RoomEndpoints : EndpointBase
{
    [HttpGet]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetAll(GetAllRoomsCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new EmptyRequest(), ct));

    [HttpGet("{id:guid}")]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetById(Guid id, GetRoomCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new GetRoomRequest { Id = new Id(id) }, ct));

    [HttpPost]
    public async Task<IResult> Create(CreateRoomCommand command, CreateRoomRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPut]
    public async Task<IResult> Update(UpdateRoomCommand command, UpdateRoomRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id, DeleteRoomCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new DeleteRoomRequest { Id = new Id(id) }, ct));
}
