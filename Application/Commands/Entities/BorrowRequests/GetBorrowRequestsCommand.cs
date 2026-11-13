namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Чтение заявок. Единая команда для библиотекаря и читателя: видимость ограничивается
/// эндпоинтом (<c>/my</c> подставляет UserId из токена, общий роут закрыт ролью).
/// Команда не проверяет роль — это ответственность маршрута, как в GetUserRoomBooksCommand.
/// </summary>
public class GetBorrowRequestsCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetBorrowRequestsRequest, BorrowRequestResponse>, ICommand
{
    public async Task<BorrowRequestResponse> Execute(
        GetBorrowRequestsRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var repo = unitOfWork.GetRepository<BorrowRequest>();

        // GroupBy(1).Select(Count()) — трюк из SearchBooksCommand: EF транслирует в SQL COUNT(*)
        // без материализации строк. Иначе пришлось бы поднимать все заявки в память ради счётчика.
        var totalCount = (await repo.ExecuteQueryAsync(
            query => ApplyFilters(query, request)
                .GroupBy(br => 1)
                .Select(g => g.Count()),
            cancellationToken)).FirstOrDefault();

        // Проекция только нужных полей: сам BorrowRequest с навигациями тянуть незачем.
        // FullName как computed-property в SQL не транслируется — склеиваем в памяти.
        var raw = await repo.ExecuteQueryAsync(query =>
        {
            query = ApplyFilters(query, request);

            return query
                .OrderByDescending(br => br.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(br => new
                {
                    br.Id,
                    br.UserId,
                    UserLastName = br.User.LastName,
                    UserFirstName = br.User.FirstName,
                    UserMiddleName = br.User.MiddleName,
                    br.RoomBookId,
                    BookTitle = br.RoomBook.Book.Title,
                    BookAuthor = br.RoomBook.Book.Author,
                    RoomName = br.RoomBook.Room.Name,
                    br.Status,
                    br.CreatedAt,
                    br.ProcessedAt,
                    br.ProcessedByUserId,
                    // ProcessedByUser — nullable navigation. EF транслирует в LEFT JOIN,
                    // все три поля null при отсутствии обработчика.
                    ProcessedByLastName = br.ProcessedByUser != null ? br.ProcessedByUser.LastName : null,
                    ProcessedByFirstName = br.ProcessedByUser != null ? br.ProcessedByUser.FirstName : null,
                    ProcessedByMiddleName = br.ProcessedByUser != null ? br.ProcessedByUser.MiddleName : null,
                    br.Comment
                });
        }, cancellationToken);

        var dtos = raw.Select(x => new BorrowRequestDTO(
            Id: x.Id.Value,
            UserId: x.UserId.Value,
            UserFullName: JoinName(x.UserLastName, x.UserFirstName, x.UserMiddleName),
            RoomBookId: x.RoomBookId.Value,
            BookTitle: x.BookTitle,
            BookAuthor: x.BookAuthor,
            RoomName: x.RoomName,
            Status: x.Status,
            CreatedAt: x.CreatedAt,
            ProcessedAt: x.ProcessedAt,
            ProcessedByUserId: x.ProcessedByUserId?.Value,
            ProcessedByFullName: x.ProcessedByLastName is null
                ? null
                : JoinName(x.ProcessedByLastName, x.ProcessedByFirstName, x.ProcessedByMiddleName),
            Comment: x.Comment
        )).ToArray();

        // Response напрямую, не через ResponseFactory.Found: TotalCount — общее количество,
        // а не размер страницы. ResponseFactory подставил бы data.Length, что при пагинации неверно.
        return new BorrowRequestResponse(
            "Ok",
            $"Found {dtos.Length} borrow requests. Total: {totalCount}.",
            dtos,
            totalCount);
    }

    private static string JoinName(string lastName, string firstName, string? middleName)
        => $"{lastName} {firstName} {middleName}".Trim();

    private static IQueryable<BorrowRequest> ApplyFilters(
        IQueryable<BorrowRequest> query, GetBorrowRequestsRequest request)
    {
        if (request.UserId.HasValue)
        {
            var userId = new Id(request.UserId.Value);
            query = query.Where(br => br.UserId == userId);
        }

        if (request.RoomId.HasValue)
        {
            var roomId = new Id(request.RoomId.Value);
            query = query.Where(br => br.RoomBook.RoomId == roomId);
        }

        if (request.BookId.HasValue)
        {
            var bookId = new Id(request.BookId.Value);
            query = query.Where(br => br.RoomBook.BookId == bookId);
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(br => br.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.UserQuery))
        {
            var q = request.UserQuery.ToLower();
            query = query.Where(br =>
                br.User.LastName.ToLower().Contains(q) ||
                br.User.FirstName.ToLower().Contains(q) ||
                br.User.MiddleName.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(request.BookQuery))
        {
            var q = request.BookQuery.ToLower();
            query = query.Where(br => br.RoomBook.Book.Title.ToLower().Contains(q));
        }

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(br => br.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            // Включаем весь день: +1 день − 1 тик. Тот же приём, что в GetAuditLogCommand
            // и GetDiscardedBooksCommand. Npgsql ругается на Unspecified Kind без SpecifyKind.
            var to = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc)
                .AddDays(1).AddTicks(-1);
            query = query.Where(br => br.CreatedAt <= to);
        }

        return query;
    }
}