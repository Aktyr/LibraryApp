namespace LibApp.Core.Requests.Book;

public class CreateBookRequest : IAddOrUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    private int _year;
    public int Year
    {
        get => _year;
        set
        {
            if (value < 0 || value > DateTime.Now.Year + 5)
                throw new ArgumentException("Year must be between 0 and current year + 5");
            _year = value;
        }
    }

    public string Publisher { get; set; } = string.Empty;
}
