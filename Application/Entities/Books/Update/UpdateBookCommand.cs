namespace LibApp.Application.Entities.Books.Update;

public class UpdateBookCommand(IRepository<Book> bookRepo, BookValidatorAsync bookValidator)
    : ICreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault();

        if (book == null)
            throw new BookNotFoundException();

        // Создаем временную книгу для валидации
        var bookForValidation = new Book
        {
            Title = request.Title,
            Author = request.Author,
            Year = request.Year,
            Publisher = request.Publisher
        };

        // Асинхронная валидация
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