namespace LibApp.Core.Requests.Entities.Room;

public class GetRoomRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
