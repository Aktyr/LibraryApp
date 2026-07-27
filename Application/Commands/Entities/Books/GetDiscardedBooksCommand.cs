namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand(IUnitOfWork unitOfWork) : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var query = discardedRepo.GetQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(d => d.DiscardedDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(d => d.DiscardedDate <= request.ToDate.Value);
        if (request.DiscardReason.HasValue)
            query = query.Where(d => d.DiscardReason == request.DiscardReason);

        var discarded = await query
            .OrderByDescending(d => d.DiscardedDate)
            .Select(d => new DiscardedBookDTO(
                d.Id.Value,
                d.Book.Id.Value,
                d.Book.Title,
                d.Amount,
                d.DiscardedDate,
                d.DiscardReason,
                d.ApprovedBy,
                d.CompensationAmount))
            .ToArrayAsync(ct);

        return ResponseFactory.Found<DiscardedBook, DiscardedBookDTO, DiscardedBookResponse>(discarded);
    }
}