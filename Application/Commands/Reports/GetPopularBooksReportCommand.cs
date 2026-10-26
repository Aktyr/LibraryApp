namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<UserRoomBook>();

        // Сначала формируем запрос, группируя по Id и собирая нужные данные в анонимный тип
        var rawData = await repo.ExecuteQueryAsync(query =>
        {
            if (request.FromDate.HasValue)
                query = query.Where(urb => urb.BorrowDate >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(urb => urb.BorrowDate <= request.ToDate.Value);

            return query
                .GroupBy(urb => urb.RoomBook.Book.Id)
                .Select(g => new
                {
                    BookId = g.Key,
                    Title = g.First().RoomBook.Book.Title,
                    Author = g.First().RoomBook.Book.Author,
                    TotalBorrowedCount = g.Count(),
                    CurrentBorrowedCount = g.Count(urb => urb.ReturnDate == null)
                })
                .OrderByDescending(x => x.TotalBorrowedCount)
                .Take(request.TopCount ?? 10);
        }, ct);

        // Материализуем в DTO уже в памяти, где .Value безопасно
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