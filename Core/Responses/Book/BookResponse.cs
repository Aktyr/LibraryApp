namespace LibApp.Core.Responses.Book;
public record BookResponse(string Status, string Message, BookDTO[] Book) : IGetResponse;