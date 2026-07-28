namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Books")]
[Route("/api/books")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class BookEndpoints : EndpointBase
{
    [HttpGet]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetAll(GetAllBooksCommand command, EmptyRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpGet("{id:guid}")]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<IResult> GetById(GetBookCommand command, GetBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPost]
    public async Task<IResult> Create(CreateBookCommand command, CreateBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPut]
    public async Task<IResult> Update(UpdateBookCommand command, UpdateBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(DeleteBookCommand command, DeleteBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPost("/discard")]
    public async Task<IResult> DiscardBook(DiscardBookCommand command, DiscardBookRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpGet("/discarded")]
    public async Task<IResult> GetDiscardedBooks(GetDiscardedBooksCommand command, GetDiscardedBooksRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPost("/undo-discard")]
    public async Task<IResult> UndoDiscard(UndoDiscardCommand command, UndoDiscardRequest request, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));
}
