namespace LibApp.Core.Requests.Borrowing;

public class GetUserBooksRequest : IGetRequest
{
    public Guid UserId { get; set; }
}