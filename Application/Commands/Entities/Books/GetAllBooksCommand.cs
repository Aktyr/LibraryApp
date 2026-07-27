namespace LibApp.Application.Commands.Entities.Books;

public class GetAllBooksCommand(IUnitOfWork unitOfWork, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<EmptyRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        // todo использовать инклюды везде, возможно стоит поправить репозиторий для этого
        var bookRepo = unitOfWork.GetRepository<Book>();
        var books = await bookRepo
            .GetQueryable()
            .Include(b => b.RoomBook)
            .ThenInclude(rb => rb.Room)
            .ToListAsync(cancellationToken);
        var bookDTOs = books.Select(book => bookConverter.ToDto(book)).ToArray();

        // мб валидатор на отстутствие книг
        return ResponseFactory.List<Book, BookDTO, BookResponse>(bookDTOs);
    }
}