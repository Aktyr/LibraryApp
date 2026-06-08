using WebAPI.Endpoints.Service;

namespace WebAPI.Endpoints;

[Route("api/books")]
public class BookEndpoints : BaseEndpoint
{
    public BookEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet]
    public async Task<ActionResult<BookResponse>> GetAll(CancellationToken cancellationToken)
    {
        var result = await ExecuteQuery<GetAllBooksCommand, EmptyRequest, BookResponse>(
            new EmptyRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetBookRequest { Id = new Id(id) };
        var result = await ExecuteQuery<GetBookCommand, GetBookRequest, BookResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Create(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<CreateBookCommand, CreateBookRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Update(
        [FromBody] UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<UpdateBookCommand, UpdateBookRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeleteBookRequest { Id = new Id(id) };
        var result = await ExecuteCommand<DeleteBookCommand, DeleteBookRequest, BasicCreateDeleteResponse>(
            request, cancellationToken);
        return Ok(result);
    }
}