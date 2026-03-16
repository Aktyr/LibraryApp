namespace LibApp.Core.Responses.Borrowing;

public record UserBooksResponse(string Status, string Message, BorrowedBookDTO[] Books) : IGetResponse;