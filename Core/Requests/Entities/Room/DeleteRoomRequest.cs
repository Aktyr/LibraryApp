namespace LibApp.Core.Requests.Entities.Room;

public class DeleteRoomRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}
