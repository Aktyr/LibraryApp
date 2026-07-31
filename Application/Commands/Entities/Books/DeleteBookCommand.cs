namespace LibApp.Application.Commands.Entities.Books;

public class DeleteBookCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        // Загружаем книгу вместе со связанными RoomBook
        var books = await bookRepo.GetAsync(
            b => b.Id.Value == request.Id.Value,
            includePaths: IncludePaths.Book.RoomBook);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Проверяем, есть ли у книги привязки к комнатам
        if (book.RoomBook != null && book.RoomBook.Any())
            throw new LibValidationException { ExceptionDetails = ["Невозможно удалить книгу, так как она присутствует в одной или нескольких комнатах"] };

        await bookRepo.RemoveAsync(book, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<Book>();
    }
}