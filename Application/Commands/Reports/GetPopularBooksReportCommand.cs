namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        Expression<Func<UserRoomBook, bool>>? filter = null;
        if (request.FromDate.HasValue)
        {
            filter = urb => urb.BorrowDate >= request.FromDate.Value;
        }
        if (request.ToDate.HasValue)
        {
            var dateFilter = (Expression<Func<UserRoomBook, bool>>)(urb => urb.BorrowDate <= request.ToDate.Value);
            filter = filter == null ? dateFilter : ExpressionHelper.CombineAnd(filter, dateFilter);
        }

        // Получаем все записи через репозиторий (без отслеживания внутри репозитория)
        var userRoomBooks = await userRoomBookRepo.GetAsync(filter, null, 0, null);

        var result = userRoomBooks
            .GroupBy(urb => urb.RoomBook.Book.Id)
            .Select(g => new BookPopularityReportDTO(
                g.Key.Value,
                g.First().RoomBook.Book.Title,
                g.First().RoomBook.Book.Author,
                g.Count(),
                g.Count(urb => !urb.IsReturned)
            ))
            .OrderByDescending(x => x.TotalBorrowedCount)
            .Take(request.TopCount ?? 10)
            .ToArray();

        return ResponseFactory.Found<Book, BookPopularityReportDTO, BookPopularityReportResponse>(result);
    }
}