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
    : AuditedCreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
    UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = UnitOfWork.GetRepository<Book>();

        var books = await bookRepo.GetAsync(
            b => b.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Book.Images);

        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

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

        // Сравнение галереи картинок
        var oldUrls = book.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList();
        var newUrls = request.ImageUrls.ToList();
        if (!oldUrls.SequenceEqual(newUrls))
            diffLines.Add("Изменена галерея изображений");

        // Временное DTO для валидации
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher,
                                  request.Genre,
                                  [],
                                  Description: request.Description,
                                  Images: request.ImageUrls.Select((url, idx) => new BookImageDTO(Guid.NewGuid(), Guid.Empty, url, idx, idx == 0)).ToList());

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

        // Синхронизация Images: удаляем старые, добавляем новые
        book.Images.Clear();
        int order = 0;
        foreach (var url in request.ImageUrls)
        {
            book.Images.Add(new BookImage
            {
                Id = new Id(Guid.NewGuid()),
                BookId = book.Id,
                Url = url,
                SortOrder = order,
                IsMain = order == 0
            });
            order++;
        }

        await bookRepo.UpdateAsync(book, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var details = diffLines.Count > 0
            ? string.Join("\n", diffLines)
            : "Обновлена книга (без изменений)";

        var audit = new AuditEntry(
            AuditActionType.Update,
            nameof(Book),
            book.Id.Value,
            details);

        return (ResponseFactory.Updated<Book>(), audit);
    }
}