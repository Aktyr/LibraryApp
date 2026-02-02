namespace LibApp.Core.Responses.Book;

public record BookDTO(Guid Id,
                      string Title,
                      string Author,
                      int Year,
                      string Publisher); // может нужно ICollection<RoomBook>
public record BooksResponse(string Status, BookDTO[] Books) : IGetResponse;
