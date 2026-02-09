namespace LibApp.Application.Entities.Rooms;

public class GetRoomCommand(IRepository<Room> roomRepo, IConverter<Room, RoomDTO> RoomConverter)
    : IGetQuery<GetRoomRequest, RoomResponse>
{
    public async Task<RoomResponse?> Execute(GetRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();

        var roomDTO = RoomConverter.ToDto(room);
        return new RoomResponse("Ok", "Room issued successfully", [roomDTO]);
    }
}