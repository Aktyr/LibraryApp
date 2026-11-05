namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Обновляет реквизиты книги. Валидация выполняется на временной сущности,
/// чтобы не менять tracked-объект до прохождения проверок.
/// </summary>
public class UpdateBookCommand(
    IUnitOfWork unitOfWork,
    BookValidatorAsync bookValidator,
    IConverter<Book, BookDTO> bookConverter,
    IAuditService auditService)
    : ICreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        var book = await bookRepo.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (book == null) throw new BookNotFoundException();

        // Временное DTO для валидации
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher,
                                  request.Genre,
                                  [],
                                  Description: request.Description,
                                  CoverUrl: request.CoverUrl);
        var bookForValidation = bookConverter.ToEntity(bookDTO);

        var validationResult = await bookValidator.ValidateAsync(bookForValidation, cancellationToken);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Обновляем только если валидация прошла
        book.Title = request.Title;
        book.Author = request.Author;
        book.Year = request.Year;
        book.Publisher = request.Publisher;
        book.Genre = request.Genre;
        book.Description = request.Description;
        book.CoverUrl = request.CoverUrl;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await bookRepo.UpdateAsync(book, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(Book),
                book.Id.Value,
                details: $"Обновлена книга «{book.Title}»",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Updated<Book>();
    }
}