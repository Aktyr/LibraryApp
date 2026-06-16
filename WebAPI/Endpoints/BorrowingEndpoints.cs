namespace WebAPI.Endpoints;

[Route("api/borrowing")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian, UserRole.Reader)]
public class BorrowingEndpoints : BaseApiController
{
    public BorrowingEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpPost("borrow")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> BorrowBook([FromBody] BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<BorrowBookCommand, BorrowBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("return")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> ReturnBook([FromBody] ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<ReturnBookCommand, ReturnBookRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("extend")]
    public async Task<ActionResult<BasicCreateDeleteResponse>> ExtendDeadline([FromBody] ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<ExtendDeadlineCommand, ExtendDeadlineRequest, BasicCreateDeleteResponse>(request, cancellationToken);
        return Ok(result);
    }
}