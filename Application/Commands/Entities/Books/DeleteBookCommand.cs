namespace LibApp.Application.Commands.Entities.Books;

public class DeleteBookCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = UnitOfWork.GetRepository<Book>();

        // Загружаем книгу вместе со связанными RoomBook
        var books = await bookRepo.GetAsync(
            b => b.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Book.RoomBook);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Проверяем, есть ли у книги привязки к комнатам
        if (book.RoomBook != null && book.RoomBook.Any())
            throw new LibValidationException { ExceptionDetails = ["Невозможно удалить книгу, так как она присутствует в одной или нескольких комнатах"] };

        await bookRepo.RemoveAsync(book, cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.Delete,
            nameof(Book),
            book.Id.Value,
            $"Удалена книга «{book.Title}»");

        return (ResponseFactory.Deleted<Book>(), audit);
    }
}