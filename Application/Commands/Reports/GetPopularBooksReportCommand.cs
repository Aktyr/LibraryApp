namespace LibApp.Application.Commands.Reports;

public class GetPopularBooksReportCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetPopularBooksRequest, BookPopularityReportResponse>, ICommand
{
    public async Task<BookPopularityReportResponse> Execute(GetPopularBooksRequest request, CancellationToken ct)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var query = userRoomBookRepo.GetQueryable().AsNoTracking();

        if (request.FromDate.HasValue)
            query = query.Where(urb => urb.BorrowDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(urb => urb.BorrowDate <= request.ToDate.Value);

        var result = await query
            .GroupBy(urb => urb.RoomBook.Book.Id)
            .Select(g => new BookPopularityReportDTO(
                g.Key.Value,
                g.FirstOrDefault().RoomBook.Book.Title,
                g.FirstOrDefault().RoomBook.Book.Author,
                g.Count(),
                g.Count(urb => !urb.IsReturned)
            ))
            .OrderByDescending(x => x.TotalBorrowedCount)
            .Take(request.TopCount ?? 10)
            .ToListAsync(ct);

        return ResponseFactory.Found<Book, BookPopularityReportDTO, BookPopularityReportResponse>(result.ToArray());
    }
}
