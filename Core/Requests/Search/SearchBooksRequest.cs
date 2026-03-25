namespace LibApp.Core.Requests.Search;

public class SearchBooksRequest : IGetRequest
{
    public string? Query { get; set; }   // Универсальный поиск по всем полям
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public string? Publisher { get; set; }

    public bool AvailableOnly { get; set; }      // Только доступные книги
    public string? SortBy { get; set; } = "Title";
    public bool SortDescending { get; set; } = false;
}