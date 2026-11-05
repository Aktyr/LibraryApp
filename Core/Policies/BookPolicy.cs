namespace LibApp.Core.Policies;

public static class BookPolicy
{
    public const int MaxTitleLength = 100;
    public const int MaxAuthorLength = 100;
    public const int MaxGenreLength = 100;
    public const int MaxPublisherLength = 100;
    public const int MaxDescriptionLength = 2000;
    public const int MaxCoverUrlLength = 500;
    public const int MaxFutureYearOffset = 5;

    public static int MaxYear => DateTime.UtcNow.Year + MaxFutureYearOffset;

    /// <summary>
    /// Проверяет параметры книги. Description и CoverUrl необязательны:
    /// пустая строка допустима и означает «не задано». CoverUrl, если задан,
    /// должен быть абсолютным http/https-адресом.
    /// </summary>
    public static List<string> Validate(
        string? title,
        string? author,
        int year,
        string? publisher,
        string? genre,
        string? description = null,
        string? coverUrl = null)
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

        if (description?.Length > MaxDescriptionLength)
            errors.Add($"Описание не может превышать {MaxDescriptionLength} символов");

        if (!string.IsNullOrWhiteSpace(coverUrl))
        {
            if (coverUrl.Length > MaxCoverUrlLength)
            {
                errors.Add($"URL обложки не может превышать {MaxCoverUrlLength} символов");
            }
            else if (!Uri.TryCreate(coverUrl, UriKind.Absolute, out var uri) ||
                     (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add("URL обложки должен быть абсолютным http/https-адресом");
            }
        }

        return errors;
    }
}