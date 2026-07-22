namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Borrowing")]
[Route("/api/borrowing")]
[EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
public class BorrowingEndpoints : EndpointBase
{
    [HttpPost("/borrow")]
    public IResult Borrow(BorrowBookCommand command, BorrowBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPost("/return")]
    public IResult Return(ReturnBookCommand command, ReturnBookRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));

    [HttpPost("/extend")]
    public IResult Extend(ExtendDeadlineCommand command, ExtendDeadlineRequest request, CancellationToken ct) => Results.Ok(command.Execute(request, ct));
}
