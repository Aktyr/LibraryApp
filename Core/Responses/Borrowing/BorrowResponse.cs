namespace LibApp.Core.Responses.Borrowing;

public record BorrowResponse(string Status, string Message, BorrowedBookDTO[] Books) : IGetResponse;