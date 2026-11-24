namespace LibApp.Application.Commands.Reports;

/// <summary>
/// Отчёт «Текущие выдачи»: книги, которые находятся на руках на момент запроса.
/// Деактивированные читатели учитываются: их выдачи остаются частью фонда,
/// поэтому глобальный фильтр по IsDeleted обходится.
/// </summary>
public class GetCurrentIssuesReportCommand(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IGetQuery<EmptyRequest, CurrentIssueReportResponse>, ICommand
{
    public async Task<CurrentIssueReportResponse> Execute(EmptyRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<UserRoomBook>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // IgnoreQueryFilters снимает фильтр и с UserRoomBook, и с User в проекции:
        // иначе выдачи деактивированных читателей исчезнут из отчёта.
        var raw = await repo.ExecuteQueryIgnoringFiltersAsync(query => query
            .Where(urb => urb.ReturnDate == null)
            .Select(urb => new
            {
                urb.Id,
                UserId = urb.User.Id,
                urb.User.LastName,
                urb.User.FirstName,
                urb.User.MiddleName,
                urb.User.IsDeleted,
                BookTitle = urb.RoomBook.Book.Title,
                BookAuthor = urb.RoomBook.Book.Author,
                RoomName = urb.RoomBook.Room.Name,
                urb.BorrowDate,
                urb.Deadline,
                urb.Penalty
            }), ct);

        // Порядок сортируем в памяти: тот же приём, что в UserBorrowsQueries.ActiveBorrows,
        // выдачи без срока уходят в конец и в памяти, и в БД.
        var dtos = raw
            .OrderBy(x => x.Deadline ?? DateTime.MaxValue)
            .Select(x => new CurrentIssueReportDTO(
                x.Id.Value,
                x.UserId.Value,
                $"{x.LastName} {x.FirstName} {x.MiddleName}".Trim(),
                x.IsDeleted,
                x.BookTitle,
                x.BookAuthor,
                x.RoomName,
                x.BorrowDate,
                x.Deadline,
                OverdueDays(x.Deadline, now),
                x.Penalty))
            .ToArray();

        return ResponseFactory.Found<UserRoomBook, CurrentIssueReportDTO, CurrentIssueReportResponse>(dtos);
    }

    // Округление вверх, как в бейдже просрочки профиля читателя: цифры на страницах должны совпадать.
    private static int OverdueDays(DateTime? deadline, DateTime now) =>
        deadline.HasValue && deadline.Value < now 
        ? (int)Math.Ceiling((now - deadline.Value).TotalDays) 
        : 0;
}