namespace LibApp.Core.Requests.Entities.Book;

public class GetBookRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
