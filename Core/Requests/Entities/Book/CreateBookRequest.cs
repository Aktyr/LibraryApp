namespace LibApp.Core.Requests.Entities.Book;

public class CreateBookRequest : IAddOrUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;

    /// <summary>Аннотация. Необязательна.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>URL обложки. Необязателен.</summary>
    public string CoverUrl { get; set; } = string.Empty;
}