namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<DiscardedBook>();

        // ToDate приходит как дата без времени (00:00). Чтобы включить весь день,
        // берём конец суток: +1 день − 1 тик. Kind приводим к Utc — DiscardedDate
        // хранится в UTC (см. DateTimeToUtcConverter), иначе Npgsql ругается на Unspecified.
        var from = request.FromDate.HasValue
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var to = request.ToDate.HasValue
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc).AddDays(1).AddTicks(-1)
            : (DateTime?)null;

        var raw = await repo.ExecuteQueryAsync(query =>
        {
            if (from.HasValue)
                query = query.Where(d => d.DiscardedDate >= from.Value);
            if (to.HasValue)
                query = query.Where(d => d.DiscardedDate <= to.Value);
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