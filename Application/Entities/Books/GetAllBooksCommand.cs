namespace LibApp.Application.Entities.Books;

public class GetAllBooksCommand(IRepository<Book> bookRepo)
    : IGetQuery<EmptyRequest, BookResponse>
{
    public async Task<BookResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var books = await bookRepo.GetWithoutTracking(cancellationToken);
        var bookDTOs = books.Select(book => new BookDTO(
            book.Id.Value,
            book.Title,
            book.Author,
            book.Year,
            book.Publisher
        )).ToArray();

        // мб валидатор на отстутствие книг

        return new BookResponse("Ok", "List of books issued successfully.", bookDTOs);
    }
}