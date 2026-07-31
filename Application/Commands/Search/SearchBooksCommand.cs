namespace LibApp.Application.Commands.Search;

public class SearchBooksCommand(
    IUnitOfWork unitOfWork,
    IConverter<Book, BookDTO> bookConverter) : IGetQuery<SearchBooksRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(SearchBooksRequest request, CancellationToken ct)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        // Формируем фильтр
        Expression<Func<Book, bool>>? filter = BuildFilter(request);

        // Формируем сортировку (кроме популярности)
        Func<IQueryable<Book>, IOrderedQueryable<Book>>? orderBy = BuildOrderBy(request);

        // Include для доступности
        var includePaths = request.AvailableOnly ? [IncludePaths.Book.RoomBook] : Array.Empty<string>();

        // Выполняем запрос
        var books = await bookRepo.GetAsync(filter, orderBy, includePaths: includePaths);

        // Если AvailableOnly, фильтруем в памяти
        if (request.AvailableOnly)
        {
            books = books.Where(b => b.RoomBook.Any(rb => rb.BookCount - rb.BorrowedCount > 0));
        }

        // Сортировка по популярности
        if (string.Equals(request.SortBy, "popularity", StringComparison.OrdinalIgnoreCase))
        {
            books = request.SortDescending
                ? books.OrderByDescending(b => b.RoomBook.Sum(rb => rb.BorrowedCount))
                : books.OrderBy(b => b.RoomBook.Sum(rb => rb.BorrowedCount));
        }

        var bookDTOs = books.Select(b => bookConverter.ToDto(b)).ToArray();
        return ResponseFactory.Found<Book, BookDTO, BookResponse>(bookDTOs);
    }

    private Expression<Func<Book, bool>>? BuildFilter(SearchBooksRequest request)
    {
        Expression<Func<Book, bool>>? filter = null;
        bool hasFilter = false;

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.ToLower();
            Expression<Func<Book, bool>> queryFilter = b =>
                b.Title.ToLower().Contains(q) ||
                b.Author.ToLower().Contains(q) ||
                b.Publisher.ToLower().Contains(q) ||
                b.Genre.ToLower().Contains(q);
            filter = queryFilter;
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var t = request.Title.ToLower();
            Expression<Func<Book, bool>> titleFilter = b => b.Title.ToLower().Contains(t);
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, titleFilter) : titleFilter;
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var a = request.Author.ToLower();
            Expression<Func<Book, bool>> authorFilter = b => b.Author.ToLower().Contains(a);
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, authorFilter) : authorFilter;
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            var g = request.Genre.ToLower();
            Expression<Func<Book, bool>> genreFilter = b => b.Genre.ToLower().Contains(g);
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, genreFilter) : genreFilter;
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Publisher))
        {
            var p = request.Publisher.ToLower();
            Expression<Func<Book, bool>> publisherFilter = b => b.Publisher.ToLower().Contains(p);
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, publisherFilter) : publisherFilter;
            hasFilter = true;
        }

        if (request.YearFrom.HasValue)
        {
            Expression<Func<Book, bool>> yearFromFilter = b => b.Year >= request.YearFrom.Value;
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, yearFromFilter) : yearFromFilter;
            hasFilter = true;
        }

        if (request.YearTo.HasValue)
        {
            Expression<Func<Book, bool>> yearToFilter = b => b.Year <= request.YearTo.Value;
            filter = hasFilter ? ExpressionHelper.CombineAnd(filter!, yearToFilter) : yearToFilter;
            hasFilter = true;
        }

        // AvailableOnly не добавляем в фильтр, т.к. требует работы с коллекцией – оставим в памяти
        return filter;
    }

    private Func<IQueryable<Book>, IOrderedQueryable<Book>>? BuildOrderBy(SearchBooksRequest request)
    {
        if (string.IsNullOrEmpty(request.SortBy))
            return q => q.OrderBy(b => b.Title);

        switch (request.SortBy.ToLower())
        {
            case "title":
                return request.SortDescending ? q => q.OrderByDescending(b => b.Title) : q => q.OrderBy(b => b.Title);
            case "author":
                return request.SortDescending ? q => q.OrderByDescending(b => b.Author) : q => q.OrderBy(b => b.Author);
            case "year":
                return request.SortDescending ? q => q.OrderByDescending(b => b.Year) : q => q.OrderBy(b => b.Year);
            case "publisher":
                return request.SortDescending ? q => q.OrderByDescending(b => b.Publisher) : q => q.OrderBy(b => b.Publisher);
            case "popularity":
                // Для сортировки по популярности требуется вычисление суммы BorrowedCount.
                // В текущей реализации оставляем сортировку по умолчанию.
                return request.SortDescending ? q => q.OrderByDescending(b => b.Title) : q => q.OrderBy(b => b.Title);
            default:
                return q => q.OrderBy(b => b.Title);
        }
    }
}