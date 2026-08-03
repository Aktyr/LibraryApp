namespace LibApp.Application.Commands.Entities.Books;

public class GetBookCommand(IUnitOfWork unitOfWork, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<GetBookRequest, BookResponse>, ICommand
{
    public async Task<BookResponse?> Execute(GetBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();
        var book = await bookRepo.FirstOrDefaultAsync(b => b.Id.Value == request.Id.Value, cancellationToken);
        if (book == null) throw new BookNotFoundException();

        var bookDto = bookConverter.ToDto(book);
        return ResponseFactory.Single<Book, BookDTO, BookResponse>(bookDto);
    }
}