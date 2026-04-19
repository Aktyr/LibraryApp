namespace LibApp.Application.Commands.Entities.Books;

public class GetAllBooksCommand(IRepository<Book> bookRepo, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<EmptyRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var books = await bookRepo.GetWithoutTracking(cancellationToken);
        var bookDTOs = books.Select(book => bookConverter.ToDto(book)).ToArray();   

        // мб валидатор на отстутствие книг
        return new BookResponse("Ok", "List of books issued successfully.", bookDTOs);
    }
}