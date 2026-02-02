namespace LibApp.Core.Requests.Book;

public class GetBookRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
