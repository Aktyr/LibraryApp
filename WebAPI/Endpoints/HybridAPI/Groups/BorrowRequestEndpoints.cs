namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("BorrowRequests")]
[Route("/api/borrow-requests")]
public class BorrowRequestEndpoints : EndpointBase
{
    // Заявку может создать любой аутентифицированный пользователь, включая
    // библиотекаря и админа: они тоже читатели и могут запрашивать книги для себя.
    [HttpPost]
    [EnumAuthorize(UserRole.Reader, UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> Create(
        CreateBorrowRequestCommand command,
        CreateBorrowRequest request,
        CancellationToken ct)
        => Results.Ok(await command.Execute(request, ct));

    // UserId принудительно из токена: query не принимает userId,
    // поэтому подмена невозможна на уровне маршрута, не только команды.
    [HttpGet("my")]
    [EnumAuthorize(UserRole.Reader, UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> GetMy(
        [FromQuery] BorrowRequestStatus? status,
        [FromQuery] Guid? bookId,
        [FromQuery] Guid? roomId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        GetBorrowRequestsCommand command,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.LibraryUserId is null) return Results.Unauthorized();

        var request = new GetBorrowRequestsRequest
        {
            UserId = currentUser.LibraryUserId.Value,
            Status = status,
            BookId = bookId,
            RoomId = roomId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        };
        return Results.Ok(await command.Execute(request, ct));
    }

    // Общий роут — только Librarian/Admin. Reader сюда не попадёт,
    // значит фильтр по userId (страница /users/{id}) легитимен.
    [HttpGet]
    [EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? bookId,
        [FromQuery] BorrowRequestStatus? status,
        [FromQuery] string? userQuery,
        [FromQuery] string? bookQuery,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        GetBorrowRequestsCommand command,
        CancellationToken ct)
    {
        var request = new GetBorrowRequestsRequest
        {
            UserId = userId,
            RoomId = roomId,
            BookId = bookId,
            Status = status,
            UserQuery = userQuery,
            BookQuery = bookQuery,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        };
        return Results.Ok(await command.Execute(request, ct));
    }

    // id из route перезаписывает BorrowRequestId в теле — та же защита,
    // что в ChangeStatus: подмена в body невозможна.
    [HttpPost("{id:guid}/approve")]
    [EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> Approve(
        Guid id,
        ApproveBorrowRequest request,
        ApproveBorrowRequestCommand command,
        CancellationToken ct)
    {
        request.BorrowRequestId = id;
        return Results.Ok(await command.Execute(request, ct));
    }

    [HttpPost("{id:guid}/reject")]
    [EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> Reject(
        Guid id,
        RejectBorrowRequest request,
        RejectBorrowRequestCommand command,
        CancellationToken ct)
    {
        request.BorrowRequestId = id;
        return Results.Ok(await command.Execute(request, ct));
    }

    // Оформление фактической выдачи по брони. Только библиотекарь/админ:
    // это операция сотрудника (читатель физически пришёл в библиотеку).
    [HttpPost("{id:guid}/issue")]
    [EnumAuthorize(UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> Issue(
        Guid id,
        IssueBorrowRequest request,
        IssueBorrowRequestCommand command,
        CancellationToken ct)
    {
        request.BorrowRequestId = id;
        return Results.Ok(await command.Execute(request, ct));
    }

    // Отмена заявки читателем. Доступно всем аутентифицированным: библиотекарь
    // тоже может отменить свою бронь. Команда проверяет, что UserId совпадает
    // с владельцем заявки, — подменить цель через этот эндпоинт нельзя.
    [HttpPost("{id:guid}/cancel")]
    [EnumAuthorize(UserRole.Reader, UserRole.Librarian, UserRole.Admin)]
    public async Task<IResult> Cancel(
        Guid id,
        CancelBorrowRequest request,
        CancelBorrowRequestCommand command,
        CancellationToken ct)
    {
        request.BorrowRequestId = id;
        return Results.Ok(await command.Execute(request, ct));
    }
}