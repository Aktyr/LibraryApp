namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    private readonly IRepository<DiscardedBook> _discardedRepo;

    public GetDiscardedBooksCommand(IRepository<DiscardedBook> discardedRepo)
    {
        _discardedRepo = discardedRepo;
    }

    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        Expression<Func<DiscardedBook, bool>> predicate = d => true;

        if (request.FromDate.HasValue)
            predicate = d => d.DiscardedDate >= request.FromDate.Value;
        if (request.ToDate.HasValue)
            predicate = d => d.DiscardedDate <= request.ToDate.Value;
        if (request.DiscardReason.HasValue)
            predicate = d => d.DiscardReason == request.DiscardReason;  

        var discarded = await _discardedRepo.Get(predicate, ct);

        var result = discarded
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
        .ToArray();
        return ResponseFactory.Found<DiscardedBook, DiscardedBookDTO, DiscardedBookResponse>(result);
    }
}