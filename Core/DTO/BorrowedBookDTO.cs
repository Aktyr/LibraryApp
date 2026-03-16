namespace LibApp.Core.DTO;

public record BorrowedBookDTO(
    Guid Id,
    string BookTitle,
    string BookAuthor,
    string RoomName,
    DateTime IssueDate,
    DateTime? DueDate,
    DateTime? ReturnDate,
    decimal? Penalty
)
{
    public BorrowedBookDTO() : this(
        default, default!, default!, default!,
        default, default, default, default)
    { }
}