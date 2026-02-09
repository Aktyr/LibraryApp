namespace LibApp.Application.Entities.Books;

public class CreateBookCommand(IRepository<Book> bookRepo, BookValidatorAsync bookValidator, IConverter<Book, BookDTO> bookConverter)
    : ICreateOrUpdateCommand<CreateBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateBookRequest request, CancellationToken cancellationToken)
    {
        // Создание
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher);
        var book = bookConverter.ToEntity(bookDTO);

        // Валидация
        var validationResult = await bookValidator.ValidateAsync(book, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Добавление 
        await bookRepo.Add(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book is created.");
    }
}