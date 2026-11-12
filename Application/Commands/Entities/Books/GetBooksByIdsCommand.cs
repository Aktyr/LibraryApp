namespace LibApp.Application.Commands.Entities.Books;

public class GetBooksByIdsCommand(IUnitOfWork unitOfWork, IConverter<Book, BookDTO> bookConverter)
    : IGetQuery<GetBooksByIdsRequest, BookResponse>, ICommand
{
    public async Task<BookResponse> Execute(GetBooksByIdsRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || !request.Ids.Any())
            return ResponseFactory.List<Book, BookDTO, BookResponse>([]);

        var bookRepo = unitOfWork.GetRepository<Book>();
        var ids = request.Ids.Select(id => new Id(id)).ToList();

        // Загружаем только нужные книги вместе с картинками и комнатами
        var books = await bookRepo.GetWithoutTrackingAsync(
            filter: b => ids.Contains(b.Id),
            cancellationToken: cancellationToken,
            includePaths: [IncludePaths.Book.RoomBook, IncludePaths.Book.Images]);

        var bookDTOs = books.Select(book => bookConverter.ToDto(book)).ToArray();

        return ResponseFactory.List<Book, BookDTO, BookResponse>(bookDTOs);
    }
}