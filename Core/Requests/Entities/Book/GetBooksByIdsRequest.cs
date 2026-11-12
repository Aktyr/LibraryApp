namespace LibApp.Core.Requests.Entities.Book;

public class GetBooksByIdsRequest : IGetRequest
{
    public IEnumerable<Guid> Ids { get; set; } = [];
}