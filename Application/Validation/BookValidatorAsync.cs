namespace LibApp.Application.Validation;

public class BookValidatorAsync
{
    public async Task<ValidationResult> ValidateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(book.Title))
            errors.Add("Название книги обязательно");

        if (string.IsNullOrWhiteSpace(book.Author))
            errors.Add("Автор книги обязателен");

        if (book.Year < 0 || book.Year > DateTime.Now.Year + 5)
            errors.Add($"Год должен быть между 0 и {DateTime.Now.Year + 5}");

        if (string.IsNullOrWhiteSpace(book.Publisher))
            errors.Add("Издательство обязательно");

        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }

}
