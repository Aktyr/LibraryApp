namespace LibApp.Application.Commands.Entities.Books;

public class GetDiscardedBooksCommand(IUnitOfWork unitOfWork) : IGetQuery<GetDiscardedBooksRequest, DiscardedBookResponse>, ICommand
{
    public async Task<DiscardedBookResponse> Execute(GetDiscardedBooksRequest request, CancellationToken ct)
    {
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        Expression<Func<DiscardedBook, bool>>? filter = null;
        bool hasFilter = false;

        if (request.FromDate.HasValue)
        {
            filter = d => d.DiscardedDate >= request.FromDate.Value;
            hasFilter = true;
        }
        if (request.ToDate.HasValue)
        {
            Expression<Func<DiscardedBook, bool>> dateFilter = d => d.DiscardedDate <= request.ToDate.Value;
            if (hasFilter)
                filter = filter == null ? dateFilter : ExpressionHelper.CombineAnd(filter!, dateFilter);
            else
                filter = dateFilter;
            hasFilter = true;
        }
        if (request.DiscardReason.HasValue)
        {
            Expression<Func<DiscardedBook, bool>> reasonFilter = d => d.DiscardReason == request.DiscardReason.Value;
            if (hasFilter)
                filter = filter == null ? reasonFilter : ExpressionHelper.CombineAnd(filter!, reasonFilter);
            else
                filter = reasonFilter;
            hasFilter = true;
        }

        Func<IQueryable<DiscardedBook>, IOrderedQueryable<DiscardedBook>> orderBy = q => q.OrderByDescending(d => d.DiscardedDate);

        var discarded = await discardedRepo.GetAsync(filter, orderBy);

        var dtos = discarded.Select(d => new DiscardedBookDTO(
            d.Id.Value,
            d.Book.Id.Value,
            d.Book.Title,
            d.Amount,
            d.DiscardedDate,
            d.DiscardReason,
            d.ApprovedBy,
            d.CompensationAmount)).ToArray();

        return ResponseFactory.Found<DiscardedBook, DiscardedBookDTO, DiscardedBookResponse>(dtos);
    }    
}