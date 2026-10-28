namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Books")]
[Route("/api/books")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class BookEndpoints : EndpointBase
{
    [HttpGet]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetAll(GetAllBooksCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new EmptyRequest(), ct));

    [HttpGet("{id:guid}")]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetById(Guid id, GetBookCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new GetBookRequest { Id = new Id(id) }, ct));

    [HttpPost]
    public async Task<IResult> Create(CreateBookCommand command, CreateBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPut]
    public async Task<IResult> Update(UpdateBookCommand command, UpdateBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id, DeleteBookCommand command, CancellationToken ct) => Results.Ok(await command.Execute(new DeleteBookRequest { Id = new Id(id) }, ct));

    [HttpPost("/discard")]
    public async Task<IResult> DiscardBook(DiscardBookCommand command, DiscardBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpGet("/discarded")]
    public async Task<IResult> GetDiscardedBooks(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] DiscardReason? discardReason,
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? approvedById,
        [FromQuery] Guid? bookId,
        GetDiscardedBooksCommand command,
        CancellationToken ct)
    {
        var request = new GetDiscardedBooksRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            DiscardReason = discardReason,
            RoomId = roomId,
            ApprovedById = approvedById,
            BookId = bookId
        };
        return Results.Ok(await command.Execute(request, ct));
    }

    [HttpPost("/undo-discard")]
    public async Task<IResult> UndoDiscard(UndoDiscardCommand command, UndoDiscardRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));
}