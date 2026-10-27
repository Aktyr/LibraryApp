namespace LibApp.Application.Commands.Reports;

public class GetUserActivityReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetUserActivityRequest, UserActivityReportResponse>, ICommand
{
    public async Task<UserActivityReportResponse> Execute(GetUserActivityRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<User>();

        // Всё в одном SQL-запросе: фильтры, сортировка, агрегация по навигации RoomBooks.
        // Пользователи без выдач попадают через левое соединение (Count/Sum/Any по пустой = 0/false).
        var rawData = await repo.ExecuteQueryAsync(query =>
        {
            if (request.OnlyWithOverdue)
                query = query.Where(u => u.RoomBooks.Any(urb =>
                    urb.ReturnDate == null && urb.Deadline.HasValue && urb.Deadline < DateTime.UtcNow));

            if (request.OnlyActive)
                query = query.Where(u => u.RoomBooks.Any(urb => urb.ReturnDate == null));

            return query
                .OrderByDescending(u => u.RoomBooks.Sum(urb => (decimal?)urb.Penalty) ?? 0)
                .Select(u => new
                {
                    Id = u.Id, // record Id, .Value берём в памяти
                    u.LastName,
                    u.FirstName,
                    u.MiddleName,
                    u.Email,
                    TotalBorrowedCount = u.RoomBooks.Count(),
                    CurrentBorrowedCount = u.RoomBooks.Count(urb => urb.ReturnDate == null),
                    TotalPenalty = u.RoomBooks.Sum(urb => (decimal?)urb.Penalty) ?? 0,
                    HasOverdue = u.RoomBooks.Any(urb =>
                        urb.ReturnDate == null && urb.Deadline.HasValue && urb.Deadline < DateTime.UtcNow)
                });
        }, ct);

        var dtos = rawData.Select(x => new UserActivityReportDTO(
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