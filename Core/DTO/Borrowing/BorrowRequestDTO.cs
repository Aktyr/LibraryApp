namespace LibApp.Core.DTO.Borrowing;

public record BorrowRequestDTO(
    Guid Id,
    Guid UserId,
    string UserFullName,
    Guid RoomBookId,
    string BookTitle,
    string BookAuthor,
    string RoomName,
    BorrowRequestStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    Guid? ProcessedByUserId,
    string? ProcessedByFullName,
    DateTime? ExpiresAt,
    DateTime? PickedUpAt,
    string? Comment)
{
    public BorrowRequestDTO() : this(
        default, default, default!, default, default!, default!, default!,
        default, default, default, default, default, default, default, default!)
    { }
}