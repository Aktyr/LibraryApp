namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Создаёт книгу. Валидирует данные и фиксирует действие в журнале аудита.
/// </summary>
public class CreateBookCommand(
    IUnitOfWork unitOfWork,
    BookValidatorAsync bookValidator,
    IConverter<Book, BookDTO> bookConverter,
    IAuditService auditService) : AuditedCreateOrUpdateCommand<CreateBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
    CreateBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = UnitOfWork.GetRepository<Book>();

        // Создание
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher,
                                  request.Genre,
                                  [],
                                  Description: request.Description,
                                  Images: request.ImageUrls.Select((url, idx) => new BookImageDTO(Guid.NewGuid(), Guid.Empty, url, idx, idx == 0)).ToList());

        var book = bookConverter.ToEntity(bookDTO);

        // Валидация
        var validationResult = await bookValidator.ValidateAsync(book, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        await bookRepo.AddAsync(book, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var audit = new AuditEntry(
            AuditActionType.Create,
            nameof(Book),
            book.Id.Value,
            $"Создана книга «{book.Title}» ({book.Author}, {book.Year})");

        return (ResponseFactory.Created<Book>(), audit);
    }
}