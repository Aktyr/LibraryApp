namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IRepository<UserRoomBook> userRoomBookRepo) : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var allBorrows = await userRoomBookRepo.Get(ct);

        var query = allBorrows.AsEnumerable();
        if (request.FromDate.HasValue)
            query = query.Where(urb => urb.BorrowDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(urb => urb.BorrowDate <= request.ToDate.Value);

        var popularBooks = allBorrows
            .GroupBy(urb => urb.RoomBook.Book.Id.Value)
            .Select(g => new BookPopularityReportDTO(
                BookId: g.Key,
                Title: g.First().RoomBook.Book.Title,
                Author: g.First().RoomBook.Book.Author,
                TotalBorrowedCount: g.Count(),
                CurrentBorrowedCount: g.Count(urb => !urb.IsReturned)
            ))
            .OrderByDescending(x => x.TotalBorrowedCount)
            .Take(request.TopCount ?? 10)
            .ToArray();

        return ResponseFactory.Found<Book, BookPopularityReportDTO, BookPopularityReportResponse>(popularBooks);
    }
}
