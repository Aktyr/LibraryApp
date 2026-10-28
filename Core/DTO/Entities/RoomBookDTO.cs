namespace LibApp.Core.DTO.Entities;

public record RoomBookDTO(Guid Id,
                          Guid RoomId,
                          Guid BookId,
                          int BookCount,
                          int BorrowedCount = 0)
{
    public RoomBookDTO() : this(default, default, default, default, default) { }
    public int AvailableCount => BookCount - BorrowedCount;
}