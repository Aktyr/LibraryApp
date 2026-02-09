namespace LibApp.Application.Entities.Books;

public class UpdateBookCommand(IRepository<Book> bookRepo, BookValidatorAsync bookValidator, IConverter<Book, BookDTO> bookConverter)
    : ICreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Временное DTO для валидации
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher);
        var bookForValidation = bookConverter.ToEntity(bookDTO);   

        var validationResult = await bookValidator.ValidateAsync(bookForValidation, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Обновляем только если валидация прошла
        book.Title = request.Title;
        book.Author = request.Author;
        book.Year = request.Year;
        book.Publisher = request.Publisher;

        await bookRepo.Update(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book updated successfully.");
    }
}