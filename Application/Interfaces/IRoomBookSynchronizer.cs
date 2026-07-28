namespace LibApp.Application.Interfaces;

public interface IRoomBookSynchronizer
{
    Task SynchronizeAsync(Room room, ICollection<RoomBookDTO> dtoList, CancellationToken cancellationToken = default);
}