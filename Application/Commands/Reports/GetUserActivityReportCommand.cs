namespace LibApp.Application.Commands.Reports;

public class GetUserActivityReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetUserActivityRequest, UserActivityReportResponse>, ICommand
{
    public async Task<UserActivityReportResponse> Execute(GetUserActivityRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<User>();

        // Границы периода: дата приходит без времени, поэтому ToDate расширяем до конца суток.
        // Kind приводим к Utc — BorrowDate хранится в UTC (см. DateTimeToUtcConverter).
        // Без периода подставляем Min/Max: предикат остаётся в дереве выражения и не даёт
        // Npgsql получить Unspecified Kind.
        var from = request.FromDate.HasValue
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        var to = request.ToDate.HasValue
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc).AddDays(1).AddTicks(-1)
            : DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

        var hasPeriod = request.FromDate.HasValue || request.ToDate.HasValue;

        // Всё в одном SQL-запросе: фильтры, сортировка, агрегация по навигации RoomBooks.
        // Пользователи без выдач попадают через левое соединение (Count/Sum/Any по пустой = 0/false),
        // но только когда период не задан.
        // Период ограничивает и набор выдач для агрегатов, и сам список пользователей.
        var rawData = await repo.ExecuteQueryAsync(query =>
        {
            // Без этого условия фильтр лишь обнулял бы счётчики, а строки оставались бы на месте.
            if (hasPeriod)
                query = query.Where(u => u.RoomBooks.Any(urb =>
                    urb.BorrowDate >= from && urb.BorrowDate <= to));

            if (request.OnlyWithOverdue)
                query = query.Where(u => u.RoomBooks.Any(urb =>
                    urb.BorrowDate >= from && urb.BorrowDate <= to &&
                    urb.ReturnDate == null && urb.Deadline.HasValue && urb.Deadline < DateTime.UtcNow));

            if (request.OnlyActive)
                query = query.Where(u => u.RoomBooks.Any(urb =>
                    urb.BorrowDate >= from && urb.BorrowDate <= to &&
                    urb.ReturnDate == null));

            return query
                .OrderByDescending(u => u.RoomBooks
                    .Where(urb => urb.BorrowDate >= from && urb.BorrowDate <= to)
                    .Sum(urb => (decimal?)urb.Penalty) ?? 0)
                .Select(u => new
                {
                    Id = u.Id, // record Id, .Value берём в памяти
                    u.LastName,
                    u.FirstName,
                    u.MiddleName,
                    u.Email,
                    TotalBorrowedCount = u.RoomBooks.Count(urb =>
                        urb.BorrowDate >= from && urb.BorrowDate <= to),
                    CurrentBorrowedCount = u.RoomBooks.Count(urb =>
                        urb.BorrowDate >= from && urb.BorrowDate <= to && urb.ReturnDate == null),
                    TotalPenalty = u.RoomBooks
                        .Where(urb => urb.BorrowDate >= from && urb.BorrowDate <= to)
                        .Sum(urb => (decimal?)urb.Penalty) ?? 0,
                    HasOverdue = u.RoomBooks.Any(urb =>
                        urb.BorrowDate >= from && urb.BorrowDate <= to &&
                        urb.ReturnDate == null && urb.Deadline.HasValue && urb.Deadline < DateTime.UtcNow)
                });
        }, ct);

        // Досортировка в памяти: при равных штрафах строки не должны «прыгать» между запросами.
        // Сначала более активные, затем по имени — предсказуемый и воспроизводимый порядок.
        var dtos = rawData
            .OrderByDescending(x => x.TotalPenalty)
            .ThenByDescending(x => x.TotalBorrowedCount)
            .ThenBy(x => x.LastName, StringComparer.CurrentCulture)
            .ThenBy(x => x.FirstName, StringComparer.CurrentCulture)
            .Select(x => new UserActivityReportDTO(
                x.Id.Value,
                $"{x.LastName} {x.FirstName} {x.MiddleName}".Trim(),
                x.Email,
                x.TotalBorrowedCount,
                x.CurrentBorrowedCount,
                x.TotalPenalty,
                x.HasOverdue
            )).ToArray();

        return ResponseFactory.Found<User, UserActivityReportDTO, UserActivityReportResponse>(dtos);
    }
}