namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<UserRoomBook>();
        var data = await repo.ExecuteQueryAsync(query =>
        {
            if (request.FromDate.HasValue)
                query = query.Where(urb => urb.BorrowDate >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(urb => urb.BorrowDate <= request.ToDate.Value);

            return query
                .GroupBy(urb => urb.RoomBook.Book.Id)
                .Select(g => new BookPopularityReportDTO(
                    g.Key.Value,
                    g.First().RoomBook.Book.Title,
                    g.First().RoomBook.Book.Author,
                    g.Count(),
                    g.Count(urb => !urb.IsReturned)))
                .OrderByDescending(x => x.TotalBorrowedCount)
                .Take(request.TopCount ?? 10);
        }, ct);

        return ResponseFactory.Found<Book, BookPopularityReportDTO, BookPopularityReportResponse>(data.ToArray());
    }
}