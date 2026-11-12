namespace LibApp.Core.Responses.Entities;
public record BookResponse(string Status, string Message, BookDTO[] Book, int TotalCount = 0) : IGetResponse;