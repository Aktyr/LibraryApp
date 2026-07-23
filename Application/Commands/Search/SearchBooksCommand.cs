namespace LibApp.Application.Commands.Search;

// todo перенести логику отчётов и поиска на EFCore
public class SearchBooksCommand : IGetQuery<SearchBooksRequest, BookResponse>, ICommand
{
    private readonly IRepository<Book> _bookRepo;
    private readonly IConverter<Book, BookDTO> _bookConverter;

    public SearchBooksCommand(IRepository<Book> bookRepo, IConverter<Book, BookDTO> bookConverter)
    {
        _bookRepo = bookRepo;
        _bookConverter = bookConverter;
    }

    public async Task<BookResponse> Execute(SearchBooksRequest request, CancellationToken ct)
    {
        var query = _bookRepo.GetQueryable();

        // Текстовые фильтры (регистронезависимый поиск)
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
        if (!string.IsNullOrWhiteSpace(request.Publisher))
        {
            var p = request.Publisher.ToLower();
            query = query.Where(b => b.Publisher.ToLower().Contains(p));
        }
        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            var g = request.Genre.ToLower();
            query = query.Where(b => b.Genre.ToLower().Contains(g));
        }

        // Фильтры по году
        if (request.YearFrom.HasValue)
            query = query.Where(b => b.Year >= request.YearFrom.Value);
        if (request.YearTo.HasValue)
            query = query.Where(b => b.Year <= request.YearTo.Value);

        // Доступность: есть хотя бы один экземпляр с AvailableCount > 0
        if (request.AvailableOnly)
            query = query.Where(b => b.RoomBook.Any(rb => rb.BookCount - rb.BorrowedCount > 0));

        // Сортировка
        IQueryable<Book> sortedQuery;
        switch (request.SortBy?.ToLower())
        {
            case "title":
                sortedQuery = request.SortDescending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title);
                break;
            case "author":
                sortedQuery = request.SortDescending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author);
                break;
            case "year":
                sortedQuery = request.SortDescending ? query.OrderByDescending(b => b.Year) : query.OrderBy(b => b.Year);
                break;
            case "publisher":
                sortedQuery = request.SortDescending ? query.OrderByDescending(b => b.Publisher) : query.OrderBy(b => b.Publisher);
                break;
            case "popularity":
                sortedQuery = request.SortDescending
                    ? query.OrderByDescending(b => b.RoomBook.Sum(rb => rb.BorrowedCount))
                    : query.OrderBy(b => b.RoomBook.Sum(rb => rb.BorrowedCount));
                break;
            default:
                sortedQuery = query.OrderBy(b => b.Title);
                break;
        }

        var books = await sortedQuery.ToListAsync(ct);
        var bookDTOs = books.Select(b => _bookConverter.ToDto(b)).ToArray();
        return ResponseFactory.Found<Book, BookDTO, BookResponse>(bookDTOs);
    }
}