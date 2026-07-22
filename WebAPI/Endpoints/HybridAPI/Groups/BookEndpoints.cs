namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Books")]
[Route("/api/books")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class BookEndpoints : EndpointBase
{
    [HttpGet]
    [EnumAuthorize(UserRole.Reader)]
    public IResult GetAll(GetAllBooksCommand command, EmptyRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpGet("{id:guid}")]
    [EnumAuthorize(UserRole.Reader)]
    public IResult GetById(GetBookCommand command, GetBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPost]
    public IResult Create(CreateBookCommand command, CreateBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPut]
    public IResult Update(UpdateBookCommand command, UpdateBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpDelete("{id:guid}")]
    public IResult Delete(DeleteBookCommand command, DeleteBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPost("/discard")]
    public IResult DiscardBook(DiscardBookCommand command, DiscardBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpGet("/discarded")]
    public IResult GetDiscardedBooks(GetDiscardedBooksCommand command, GetDiscardedBooksRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPost("/undo-discard")]
    public IResult UndoDiscard(UndoDiscardCommand command, UndoDiscardRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));
}
