namespace LibApp.Core.Policies;

public static class BookPolicy
{
    public const int MaxTitleLength = 100;
    public const int MaxAuthorLength = 100;
    public const int MaxGenreLength = 100;
    public const int MaxPublisherLength = 100;
    public const int MaxFutureYearOffset = 5;

    public static int MaxYear => DateTime.UtcNow.Year + MaxFutureYearOffset;

    public static List<string> Validate(string? title, string? author, int year, string? publisher, string? genre)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
            errors.Add("Название книги обязательно");
        else if (title.Length > MaxTitleLength)
            errors.Add($"Название книги не может превышать {MaxTitleLength} символов");

        if (string.IsNullOrWhiteSpace(author))
            errors.Add("Автор книги обязателен");
        else if (author.Length > MaxAuthorLength)
            errors.Add($"Имя Автора не может превышать {MaxAuthorLength} символов");

        if (year < 0 || year > MaxYear)
            errors.Add($"Год должен быть между 0 и {MaxYear}");

        if (string.IsNullOrWhiteSpace(publisher))
            errors.Add("Издательство обязательно");

        if (genre?.Length > MaxGenreLength)
            errors.Add($"Название жанра не может превышать {MaxGenreLength} символов");

        return errors;
    }
}