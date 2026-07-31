namespace LibApp.Application.Commands.Entities.Books;

public class CreateBookCommand(
    IUnitOfWork unitOfWork,
    BookValidatorAsync bookValidator,
    IConverter<Book, BookDTO> bookConverter) : ICreateOrUpdateCommand<CreateBookRequest, BasicCreateDeleteResponse>, ICommand
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

        // Добавление 
        await bookRepo.AddAsync(book, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ResponseFactory.Created<Book>();
    }
}