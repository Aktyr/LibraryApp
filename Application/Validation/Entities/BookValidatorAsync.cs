namespace LibApp.Application.Validation.Entities;

public class BookValidatorAsync : IValidator
{
    public async Task<ValidationResult> ValidateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(book.Title))
            errors.Add("Название книги обязательно");

        if (string.IsNullOrWhiteSpace(book.Author))
            errors.Add("Автор книги обязателен");

        if (book.Year < 0 || book.Year > DateTime.Now.Year + 5) // todo возможно придётся поменять DateTime.Now на что-то другое
            errors.Add($"Год должен быть между 0 и {DateTime.Now.Year + 5}");

        if (string.IsNullOrWhiteSpace(book.Publisher))
            errors.Add("Издательство обязательно");

        if (book.Title.Length > 100)
            errors.Add("Название книги не может превышать 100 символов");

        if (book.Author.Length > 100)
            errors.Add("Имя Автора не может превышать 100 символов");


        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }

}
