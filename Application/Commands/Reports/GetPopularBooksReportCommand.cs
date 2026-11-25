namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<UserRoomBook>();

        // Границы периода: дата приходит без времени, поэтому ToDate расширяем до конца суток.
        // Kind приводим к Utc — BorrowDate хранится в UTC (см. DateTimeToUtcConverter),
        // иначе Npgsql ругается на Unspecified.
        var from = request.FromDate.HasValue
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var to = request.ToDate.HasValue
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc).AddDays(1).AddTicks(-1)
            : (DateTime?)null;

        // Формируем запрос, материализуем в анонимный тип
        var rawData = await repo.ExecuteQueryAsync(query =>
        {
            if (from.HasValue)
                query = query.Where(urb => urb.BorrowDate >= from.Value);
            if (to.HasValue)
                query = query.Where(urb => urb.BorrowDate <= to.Value);

            var ordered = query
                .GroupBy(urb => urb.RoomBook.Book.Id)
                .Select(g => new
                {
                    BookId = g.Key,
                    Title = g.First().RoomBook.Book.Title,
                    Author = g.First().RoomBook.Book.Author,
                    TotalBorrowedCount = g.Count(),
                    CurrentBorrowedCount = g.Count(urb => urb.ReturnDate == null)
                })
                .OrderByDescending(x => x.TotalBorrowedCount);

            // Take после OrderBy: иначе LIMIT срежет произвольные строки до сортировки.
            // TopCount == null — без усечения: клиент сортирует полный набор строк.
            return request.TopCount.HasValue ? ordered.Take(request.TopCount.Value) : ordered;
        }, ct);

        // Конвертируем в DTO в памяти
        var dtos = rawData.Select(x => new BookPopularityReportDTO(
            x.BookId.Value,
            x.Title,
            x.Author,
            x.TotalBorrowedCount,
            x.CurrentBorrowedCount
        )).ToArray();

        return ResponseFactory.Found<Book, BookPopularityReportDTO, BookPopularityReportResponse>(dtos);
    }
}