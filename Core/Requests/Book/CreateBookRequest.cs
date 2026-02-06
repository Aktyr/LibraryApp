namespace LibApp.Core.Requests.Book;

public class CreateBookRequest : IAddOrUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publisher { get; set; } = string.Empty;
}