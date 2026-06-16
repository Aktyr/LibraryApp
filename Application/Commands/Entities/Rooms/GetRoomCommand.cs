namespace LibApp.Application.Commands.Entities.Rooms;

public class GetRoomCommand(IRepository<Room> roomRepo, IConverter<Room, RoomDTO> RoomConverter)
    : IGetQuery<GetRoomRequest, RoomResponse>, ICommand
{
    public async Task<RoomResponse?> Execute(GetRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();

        var roomDTO = RoomConverter.ToDto(room);

        return ResponseFactory.Single<Room, RoomDTO, RoomResponse>(roomDTO);
    }
}