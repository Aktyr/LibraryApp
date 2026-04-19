namespace LibApp.Application.Commands.Entities.Books;

public class GetBookCommand(IRepository<Book> bookRepo, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<GetBookRequest, BookResponse>, ICommand
{
    public async Task<BookResponse?> Execute(GetBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        var bookDto = bookConverter.ToDto(book);
        return new BookResponse("Ok", "Book issued successfully.", [bookDto]);
    }
}