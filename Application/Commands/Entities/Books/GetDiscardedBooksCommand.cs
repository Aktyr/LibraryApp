namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<DiscardedBook>();

        var raw = await repo.ExecuteQueryAsync(query =>
        {
            if (request.FromDate.HasValue)
                query = query.Where(d => d.DiscardedDate >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(d => d.DiscardedDate <= request.ToDate.Value);
            if (request.DiscardReason.HasValue)
                query = query.Where(d => d.DiscardReason == request.DiscardReason.Value);
            if (request.RoomId.HasValue)
                query = query.Where(d => d.RoomId == new Id(request.RoomId.Value));
            if (request.ApprovedById.HasValue)
                query = query.Where(d => d.ApprovedById == new Id(request.ApprovedById.Value));
            if (request.BookId.HasValue)
                query = query.Where(d => d.Book.Id == new Id(request.BookId.Value));

            return query
                .OrderByDescending(d => d.DiscardedDate)
                .Select(d => new
                {
                    Id = d.Id,
                    BookId = d.Book.Id,
                    BookTitle = d.Book.Title,
                    RoomId = d.RoomId,
                    RoomName = d.Room.Name,
                    d.Amount,
                    d.DiscardedDate,
                    d.DiscardReason,
                    d.ApprovedBy,
                    d.CompensationAmount
                });
        }, ct);

        var dtos = raw.Select(x => new DiscardedBookDTO(
            x.Id.Value,
            x.BookId.Value,
            x.BookTitle,
            x.RoomId.Value,
            x.RoomName,
            x.Amount,
            x.DiscardedDate,
            x.DiscardReason,
            x.ApprovedBy,
            x.CompensationAmount)).ToArray();

        return ResponseFactory.Found<DiscardedBook, DiscardedBookDTO, DiscardedBookResponse>(dtos);
    }
}