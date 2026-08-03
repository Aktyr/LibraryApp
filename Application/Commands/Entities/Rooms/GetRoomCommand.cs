namespace LibApp.Application.Commands.Entities.Rooms;

public class GetRoomCommand(IUnitOfWork unitOfWork, IConverter<Room, RoomDTO> RoomConverter)
    : IGetQuery<GetRoomRequest, RoomResponse>, ICommand
{
    public async Task<RoomResponse?> Execute(GetRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();
        var room = await roomRepo.FirstOrDefaultAsync(r => r.Id.Value == request.Id.Value, cancellationToken);
        if (room == null) throw new RoomNotFoundException();

        var roomDTO = RoomConverter.ToDto(room);

        return ResponseFactory.Single<Room, RoomDTO, RoomResponse>(roomDTO);
    }
}