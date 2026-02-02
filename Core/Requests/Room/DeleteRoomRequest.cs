namespace LibApp.Core.Requests.Room;

public class DeleteRoomRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}
