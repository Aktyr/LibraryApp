namespace LibApp.Application.Entities.Books.Read;

public class GetBookQuery(IRepository<Book> bookRepo)
    : IGetQuery<GetBookRequest, BooksResponse>
{
    public async Task<BooksResponse?> Execute(GetBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault();

        if (book == null)
            throw new BookNotFoundException();

        return new BooksResponse("Ok", "Book issued successfully.", [new(
            book.Id.Value,
            book.Title,
            book.Author,
            book.Year,
            book.Publisher)]
        );
    }
}