namespace LibApp.Application.Interfaces;

public interface IRoomBookSynchronizationValidator
{
    Task<ValidationResponse> ValidateAsync(
        Room room,
        ICollection<RoomBookDTO> dtoList,
        ICollection<RoomBook> existingRoomBooks,
        CancellationToken cancellationToken = default);
}