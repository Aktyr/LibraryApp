    namespace LibApp.Application.Commands.Entities.Books;

public class UpdateBookCommand(IUnitOfWork unitOfWork, BookValidatorAsync bookValidator, IConverter<Book, BookDTO> bookConverter)
    : ICreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();

        var book = await bookRepo.FirstOrDefaultAsync(b => b.Id.Value == request.Id.Value, cancellationToken);
        if (book == null) throw new BookNotFoundException();

        // Временное DTO для валидации
        var bookDTO = new BookDTO(Guid.NewGuid(),
                                  request.Title,
                                  request.Author,
                                  request.Year,
                                  request.Publisher,
                                  request.Genre, []);
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
        //book.RoomBook = request.RoomBook;

        await bookRepo.UpdateAsync(book, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Updated<Book>();
    }
}