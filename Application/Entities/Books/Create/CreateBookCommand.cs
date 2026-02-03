using LibApp.Application.Validation;

namespace LibApp.Application.Entities.Books.Create;

public class CreateBookCommand(IRepository<Book> bookRepo)
    : ICreateOrUpdateCommand<CreateBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateBookRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var validationResult = BookValidator.Validate(new Book
        {
            Title = request.Title,
            Author = request.Author,
            Year = request.Year,
            Publisher = request.Publisher
        });


        if (!validationResult.IsValid)        
            throw new ValidationException{ExceptionDetails = validationResult.Errors.ToList()};

        // Создание 
        var book = new Book
        {
            Author = request.Author,
            Publisher = request.Publisher,
            Title = request.Title,
            Year = request.Year,
            RoomBooks = []
        };
        await bookRepo.Add(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book is created.");
    }
}