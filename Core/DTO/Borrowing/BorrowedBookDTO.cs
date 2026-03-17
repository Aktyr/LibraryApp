namespace LibApp.Core.DTO.Borrowing;

public record BorrowedBookDTO(
    Guid Id,
    string BookTitle,
    string BookAuthor,
    string RoomName,
    DateTime Borrow,
    DateTime? Deadline,
    DateTime? ReturnDate,
    decimal? Penalty
)
{
    public BorrowedBookDTO() : this(
        default, default!, default!, default!,
        default, default, default, default)
    { }
}