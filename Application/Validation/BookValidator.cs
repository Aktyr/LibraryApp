namespace LibApp.Application.Validation;

public class BookValidator
{
    public static ValidationResult Validate(Book book)
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

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
