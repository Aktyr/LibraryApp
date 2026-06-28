namespace WebAPI.Endpoints.Controllers;

[Route("api/books")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class BookEndpoints : BaseApiController
{
    public BookEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<ActionResult<BookResponse>> GetAll(CancellationToken cancellationToken)
    {
        var result = await ExecuteQuery<GetAllBooksCommand, EmptyRequest, BookResponse>(new EmptyRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [EnumAuthorize(UserRole.Reader)]
    public async Task<ActionResult<BookResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetBookRequest { Id = new Id(id) };
        var result = await ExecuteQuery<GetBookCommand, GetBookRequest, BookResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Create([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<CreateBookCommand, CreateBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Update([FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<UpdateBookCommand, UpdateBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteBookRequest { Id = new Id(id) };
        var result = await ExecuteCommand<DeleteBookCommand, DeleteBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("discard")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> DiscardBook(
        [FromBody] DiscardBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<DiscardBookCommand, DiscardBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("discarded")]
    public async Task<ActionResult<DiscardedBookResponse>> GetDiscardedBooks(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] DiscardReason reason,
        CancellationToken cancellationToken)
    {
        var request = new GetDiscardedBooksRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            DiscardReason = reason
        };
        var result = await ExecuteQuery<GetDiscardedBooksCommand, GetDiscardedBooksRequest, DiscardedBookResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("undo-discard")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> UndoDiscard(
        [FromBody] UndoDiscardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<UndoDiscardCommand, UndoDiscardRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

}