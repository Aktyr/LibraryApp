namespace LibApp.Application.Commands.Entities.Books;

public class GetAllBooksCommand(IUnitOfWork unitOfWork, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<EmptyRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();
        var books = await bookRepo.GetWithoutTracking(cancellationToken);
        var bookDTOs = books.Select(book => bookConverter.ToDto(book)).ToArray();

        // мб валидатор на отстутствие книг
        return ResponseFactory.List<Book, BookDTO, BookResponse>(bookDTOs);
    }
}