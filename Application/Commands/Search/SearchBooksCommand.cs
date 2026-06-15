namespace LibApp.Application.Commands.Search;

// todo перенести логику отчётов и поиска на EFCore
public class SearchBooksCommand : IGetQuery<SearchBooksRequest, BookResponse>, ICommand
{
    private readonly IRepository<Book> _bookRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly IConverter<Book, BookDTO> _bookConverter;

    public SearchBooksCommand(IRepository<Book> bookRepo, IRepository<RoomBook> roomBookRepo, IConverter<Book, BookDTO> bookConverter)
    {
        _bookRepo = bookRepo;
        _roomBookRepo = roomBookRepo;
        _bookConverter = bookConverter;
    }

    public async Task<BookResponse> Execute(SearchBooksRequest request, CancellationToken cancellationToken)
    {
        var predicate = BuildSearchPredicate(request);
        var books = await _bookRepo.Get(predicate, cancellationToken);
        var booksList = books.ToList();

        // Фильтрация по доступности
        if (request.AvailableOnly)
        {
            var availableBookIds = await GetAvailableBookIds(cancellationToken);
            booksList = booksList.Where(b => availableBookIds.Contains(b.Id.Value)).ToList();
        }

        // Сортировка
        var sortedBooks = ApplySorting(booksList, request);

        // Конвертация в DTO
        var bookDTOs = sortedBooks.Select(b => _bookConverter.ToDto(b)).ToArray();

        return new BookResponse("Ok", $"Найдено книг: {bookDTOs.Length}", bookDTOs);
    }

    private Expression<Func<Book, bool>> BuildSearchPredicate(SearchBooksRequest request)
    {
        Expression<Func<Book, bool>> predicate = b => true;
        var parameter = predicate.Parameters[0];
        var body = predicate.Body;

        // Универсальный поиск по всем текстовым полям (регистронезависимый)
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var queryLower = request.Query.ToLower();

            var titleProperty = Expression.Property(parameter, nameof(Book.Title));
            var titleToLower = Expression.Call(titleProperty,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var titleContains = Expression.Call(titleToLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(queryLower));

            var authorProperty = Expression.Property(parameter, nameof(Book.Author));
            var authorToLower = Expression.Call(authorProperty,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var authorContains = Expression.Call(authorToLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(queryLower));

            var publisherProperty = Expression.Property(parameter, nameof(Book.Publisher));
            var publisherToLower = Expression.Call(publisherProperty,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var publisherContains = Expression.Call(publisherToLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(queryLower));

            var queryMatch = Expression.OrElse(titleContains,
                Expression.OrElse(authorContains, publisherContains));
            body = Expression.AndAlso(body, queryMatch);
        }

        // Поиск по названию
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var titleLower = request.Title.ToLower();
            var property = Expression.Property(parameter, nameof(Book.Title));
            var toLower = Expression.Call(property,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(titleLower));
            body = Expression.AndAlso(body, contains);
        }

        // Поиск по автору
        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var authorLower = request.Author.ToLower();
            var property = Expression.Property(parameter, nameof(Book.Author));
            var toLower = Expression.Call(property,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(authorLower));
            body = Expression.AndAlso(body, contains);
        }

        // Поиск по издательству
        if (!string.IsNullOrWhiteSpace(request.Publisher))
        {
            var publisherLower = request.Publisher.ToLower();
            var property = Expression.Property(parameter, nameof(Book.Publisher));
            var toLower = Expression.Call(property,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(publisherLower));
            body = Expression.AndAlso(body, contains);
        }

        // Поиск по жанру
        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            var genreLower = request.Genre.ToLower();
            var property = Expression.Property(parameter, nameof(Book.Genre));
            var toLower = Expression.Call(property,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower,
                typeof(string).GetMethod("Contains", [typeof(string)])!,
                Expression.Constant(genreLower));
            body = Expression.AndAlso(body, contains);
        }


        // Фильтр по году (от)
        if (request.YearFrom.HasValue)
        {
            var property = Expression.Property(parameter, nameof(Book.Year));
            var condition = Expression.GreaterThanOrEqual(property, Expression.Constant(request.YearFrom.Value));
            body = Expression.AndAlso(body, condition);
        }

        // Фильтр по году (до)
        if (request.YearTo.HasValue)
        {
            var property = Expression.Property(parameter, nameof(Book.Year));
            var condition = Expression.LessThanOrEqual(property, Expression.Constant(request.YearTo.Value));
            body = Expression.AndAlso(body, condition);
        }

        return Expression.Lambda<Func<Book, bool>>(body, parameter);
    }

    private async Task<HashSet<Guid>> GetAvailableBookIds(CancellationToken cancellationToken)
    {
        var roomBooks = await _roomBookRepo.Get(cancellationToken);
        return roomBooks
            .Where(rb => rb.AvailableCount > 0)
            .Select(rb => rb.Book.Id.Value)
            .ToHashSet();
    }

    private IEnumerable<Book> ApplySorting(IEnumerable<Book> books, SearchBooksRequest request)
    {
        return request.SortBy?.ToLower() switch
        {
            "title" => request.SortDescending
                ? books.OrderByDescending(b => b.Title)
                : books.OrderBy(b => b.Title),
            "author" => request.SortDescending
                ? books.OrderByDescending(b => b.Author)
                : books.OrderBy(b => b.Author),
            "year" => request.SortDescending
                ? books.OrderByDescending(b => b.Year)
                : books.OrderBy(b => b.Year),
            "publisher" => request.SortDescending
                ? books.OrderByDescending(b => b.Publisher)
                : books.OrderBy(b => b.Publisher),
            "popularity" => request.SortDescending
                ? books.OrderByDescending(b => b.RoomBook.Sum(rb => rb.BorrowedCount))
                : books.OrderBy(b => b.RoomBook.Sum(rb => rb.BorrowedCount)),
            _ => books.OrderBy(b => b.Title)
        };
    }
}