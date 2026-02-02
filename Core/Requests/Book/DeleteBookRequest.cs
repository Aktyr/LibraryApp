namespace LibApp.Core.Requests.Book;

public class DeleteBookRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}