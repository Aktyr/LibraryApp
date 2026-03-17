namespace LibApp.Core.Requests.Entities.Book;

public class DeleteBookRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}