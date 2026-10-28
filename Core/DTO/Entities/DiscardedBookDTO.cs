namespace LibApp.Core.DTO.Entities;

public record DiscardedBookDTO(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid RoomId,
    string RoomName,
    int Amount,
    DateTime DiscardedDate,
    DiscardReason DiscardReason,
    string? ApprovedBy,
    decimal? CompensationAmount)
{ public DiscardedBookDTO() : this(default, default, default!, default, default!, default, default, default, default!, default) { } }