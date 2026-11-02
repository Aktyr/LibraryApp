namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Создаёт книгу. Валидирует данные и фиксирует действие в журнале аудита.
/// </summary>
public class CreateBookCommand(
    IUnitOfWork unitOfWork,
    BookValidatorAsync bookValidator,
    IConverter<Book, BookDTO> bookConverter,
    IAuditService auditService) : ICreateOrUpdateCommand<CreateBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        // Создание
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher,
                                  request.Genre, []);
        var book = bookConverter.ToEntity(bookDTO);

        // Валидация
        var validationResult = await bookValidator.ValidateAsync(book, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Транзакция нужна, чтобы запись аудита попала в тот же commit, что и сама книга.
        // Если SaveChanges упадёт — журнал не останется с «фантомной» записью.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await bookRepo.AddAsync(book, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Create,
                nameof(Book),
                book.Id.Value,
                details: $"Создана книга «{book.Title}» ({book.Author}, {book.Year})",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Created<Book>();
    }
}