namespace LibApp.Application.Commands.Search;

public class SearchBooksCommand(IUnitOfWork unitOfWork)
    : IGetQuery<SearchBooksRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(SearchBooksRequest request, CancellationToken ct)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        var raw = await bookRepo.ExecuteQueryAsync(query =>
        {
            query = ApplyFilters(query, request);

            if (request.AvailableOnly)
                query = query.Where(b => b.RoomBook.Any(rb => rb.BookCount > rb.BorrowedCount));

            query = ApplySort(query, request);

            // Проекция в анонимный тип: EF транслирует навигацию RoomBook -> RoomBook.Room/Book
            // в LEFT JOIN без необходимости явных Include и без N+1.
            return query.Select(b => new
            {
                b.Id,
                b.Title,
                b.Author,
                b.Year,
                b.Publisher,
                b.Genre,
                b.IsDiscarded,
                b.Description,
                Images = b.Images.OrderBy(i => i.SortOrder).Select(i => new
                {
                    i.Id,
                    i.BookId,
                    i.Url,
                    i.SortOrder,
                    i.IsMain
                }).ToList(),
                RoomBook = b.RoomBook.Select(rb => new
                {
                    rb.Id,
                    RoomId = rb.Room.Id,
                    BookId = rb.Book.Id,
                    rb.BookCount,
                    rb.BorrowedCount
                }).ToList()
            });
        }, ct);

        var dtos = raw.Select(x => new BookDTO(
            x.Id.Value,
            x.Title,
            x.Author,
            x.Year,
            x.Publisher,
            x.Genre,
            x.RoomBook.Select(rb => new RoomBookDTO(
                rb.Id.Value,
                rb.RoomId.Value,
                rb.BookId.Value,
                rb.BookCount,
                rb.BorrowedCount
            )).ToList(),
            x.IsDiscarded,
            x.Description,
            x.Images.Select(i => new BookImageDTO(
                i.Id.Value,
                i.BookId.Value,
                i.Url,
                i.SortOrder,
                i.IsMain
            )).ToList()
        )).ToArray();

        return ResponseFactory.Found<Book, BookDTO, BookResponse>(dtos);
    }

    private static IQueryable<Book> ApplyFilters(IQueryable<Book> query, SearchBooksRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(q) ||
                b.Author.ToLower().Contains(q) ||
                b.Publisher.ToLower().Contains(q) ||
                b.Genre.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var t = request.Title.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var a = request.Author.ToLower();
            query = query.Where(b => b.Author.ToLower().Contains(a));
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            var g = request.Genre.ToLower();
            query = query.Where(b => b.Genre.ToLower().Contains(g));
        }

        if (!string.IsNullOrWhiteSpace(request.Publisher))
        {
            var p = request.Publisher.ToLower();
            query = query.Where(b => b.Publisher.ToLower().Contains(p));
        }

        if (request.YearFrom.HasValue)
            query = query.Where(b => b.Year >= request.YearFrom.Value);

        if (request.YearTo.HasValue)
            query = query.Where(b => b.Year <= request.YearTo.Value);

        return query;
    }

    private static IQueryable<Book> ApplySort(IQueryable<Book> query, SearchBooksRequest request)
    {
        // Популярность сортируется через SQL SUM по навигации RoomBook.
        // EF транслирует collection.Sum() в COALESCE(SUM(...), 0) через LEFT JOIN,
        // что позволяет делать ORDER BY прямо в БД без материализации.
        if (string.Equals(request.SortBy, "popularity", StringComparison.OrdinalIgnoreCase))
        {
            return request.SortDescending
                ? query.OrderByDescending(b => b.RoomBook.Sum(rb => rb.BorrowedCount))
                : query.OrderBy(b => b.RoomBook.Sum(rb => rb.BorrowedCount));
        }

        return request.SortBy?.ToLower() switch
        {
            null or "" => query.OrderBy(b => b.Title),
            "title" => request.SortDescending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "author" => request.SortDescending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "year" => request.SortDescending ? query.OrderByDescending(b => b.Year) : query.OrderBy(b => b.Year),
            "publisher" => request.SortDescending ? query.OrderByDescending(b => b.Publisher) : query.OrderBy(b => b.Publisher),
            _ => query.OrderBy(b => b.Title)
        };
    }
}