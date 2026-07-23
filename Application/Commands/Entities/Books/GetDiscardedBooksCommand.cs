namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand(IUnitOfWork unitOfWork) : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        Expression<Func<DiscardedBook, bool>> predicate = d => true;

        if (request.FromDate.HasValue)
            predicate = d => d.DiscardedDate >= request.FromDate.Value;
        if (request.ToDate.HasValue)
            predicate = d => d.DiscardedDate <= request.ToDate.Value;
        if (request.DiscardReason.HasValue)
            predicate = d => d.DiscardReason == request.DiscardReason;  

        var discarded = await discardedRepo.Get(predicate, ct);

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