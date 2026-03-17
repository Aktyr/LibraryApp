using LibApp.Core.DTO.Entities;
using LibApp.Core.Requests.Entities.Book;
using LibApp.Core.Responses.Entities;

namespace LibApp.Application.Commands.Entities.Books;

public class GetBookCommand(IRepository<Book> bookRepo, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<GetBookRequest, BookResponse>
{
    public async Task<BookResponse?> Execute(GetBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        var bookDto = bookConverter.ToDto(book);
        return new BookResponse("Ok", "Book issued successfully.", [bookDto]);
    }
}