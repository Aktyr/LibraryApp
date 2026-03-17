namespace LibApp.Core.Requests.Entities.Room;

public class UpdateRoomRequest : IAddOrUpdateRequest
{
    public Id Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SumOfBooks => RoomBooks.Select(x => x.BookCount).Sum(); // надо ли передавать?
    public ICollection<RoomBook> RoomBooks { get; set; } = [];
}
