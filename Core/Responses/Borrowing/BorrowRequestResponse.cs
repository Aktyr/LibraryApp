namespace LibApp.Core.Responses.Borrowing;

/// <summary>
/// Ответ со списком заявок. <see cref="TotalCount"/> — общее количество совпадений
/// по фильтрам, независимо от пагинации. <c>Data</c> — текущая страница.
/// </summary>
public record BorrowRequestResponse(
    string Status,
    string Message,
    BorrowRequestDTO[] Data,
    int TotalCount = 0) : IGetResponse;