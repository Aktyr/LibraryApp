namespace LibApp.Core.Requests.Room;

public class GetRoomRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
