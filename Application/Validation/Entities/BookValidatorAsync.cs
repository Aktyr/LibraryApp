namespace LibApp.Application.Validation.Entities;

public class BookValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var errors = BookPolicy.Validate(
            book.Title,
            book.Author,
            book.Year,
            book.Publisher,
            book.Genre,
            book.Description,
            book.CoverUrl);

        await Task.CompletedTask;
        return new ValidationResponse(errors.Count == 0, errors);
    }
}