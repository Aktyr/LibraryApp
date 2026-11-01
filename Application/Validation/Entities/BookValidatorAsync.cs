namespace LibApp.Application.Validation.Entities;

public class BookValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var errors = BookPolicy.Validate(book.Title, book.Author, book.Year, book.Publisher, book.Genre);

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }
}