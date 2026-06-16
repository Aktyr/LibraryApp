namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;

    public GetPopularBooksReportCommand(IRepository<UserRoomBook> userRoomBookRepo) 
    {
        _userRoomBookRepo = userRoomBookRepo;
    }

    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var allBorrows = await _userRoomBookRepo.Get(ct);

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
