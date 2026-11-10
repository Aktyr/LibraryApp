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

        var diffLines = new List<string>();
        void AddDiff(string field, object? oldVal, object? newVal)
        {
            var diff = AuditDiffBuilder.FormatChange(field, oldVal, newVal);
            if (!string.IsNullOrEmpty(diff)) diffLines.Add(diff);
        }

        AddDiff("Изменено название", book.Title, request.Title);
        AddDiff("Изменён автор", book.Author, request.Author);
        AddDiff("Изменён год", book.Year, request.Year);
        AddDiff("Изменено издательство", book.Publisher, request.Publisher);
        AddDiff("Изменён жанр", book.Genre, request.Genre);

        if (book.Description != request.Description) diffLines.Add("Изменено описание");
        if (book.CoverUrl != request.CoverUrl) diffLines.Add("Изменён URL обложки");

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

            var details = diffLines.Count > 0
                ? string.Join("\n", diffLines)
                : "Обновлена книга (без изменений)";

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(Book),
                book.Id.Value,
                details: details,
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Updated<Book>();
    }
}