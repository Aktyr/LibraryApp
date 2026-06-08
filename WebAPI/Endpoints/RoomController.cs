namespace WebAPI.Endpoints;

[Route("api/rooms")]
public class RoomController : BaseApiController
{
    public RoomController(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet]
    public async Task<ActionResult<RoomResponse>> GetAll(CancellationToken cancellationToken)
    {
        var result = await ExecuteQuery<GetAllRoomsCommand, EmptyRequest, RoomResponse>(
            new EmptyRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoomResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetRoomRequest { Id = new Id(id) };
        var result = await ExecuteQuery<GetRoomCommand, GetRoomRequest, RoomResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<CreateRoomCommand, CreateRoomRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Update(
        [FromBody] UpdateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<UpdateRoomCommand, UpdateRoomRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeleteRoomRequest { Id = new Id(id) };
        var result = await ExecuteCommand<DeleteRoomCommand, DeleteRoomRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }
}