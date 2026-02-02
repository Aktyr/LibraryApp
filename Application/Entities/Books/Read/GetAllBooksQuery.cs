namespace LibApp.Application.Entities.Books.Read;

public class GetAllBooksQuery(IRepository<Book> bookRepo)
    : IGetQuery<EmptyRequest, BooksResponse>
{
    public async Task<BooksResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var books = await bookRepo.GetWithoutTracking(cancellationToken);
        var bookDTOs = books.Select(book => new BookDTO(
            book.Id.Value,
            book.Title,
            book.Author,
            book.Year,
            book.Publisher
        )).ToArray();

        return new BooksResponse("Ok", bookDTOs);
    }
}