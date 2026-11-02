namespace LibApp.Application.Commands.Entities.Books;

public class DeleteBookCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : IDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        // Загружаем книгу вместе со связанными RoomBook
        var books = await bookRepo.GetAsync(
            b => b.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Book.RoomBook);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Проверяем, есть ли у книги привязки к комнатам
        if (book.RoomBook != null && book.RoomBook.Any())
            throw new LibValidationException { ExceptionDetails = ["Невозможно удалить книгу, так как она присутствует в одной или нескольких комнатах"] };

        var deletedTitle = book.Title;
        var deletedId = book.Id.Value;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await bookRepo.RemoveAsync(book, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Delete,
                nameof(Book),
                deletedId,
                details: $"Удалена книга «{deletedTitle}»",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Deleted<Book>();
    }
}