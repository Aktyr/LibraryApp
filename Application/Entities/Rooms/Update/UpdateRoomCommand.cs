namespace LibApp.Application.Entities.Rooms.Update;

public class UpdateRoomCommand(IRepository<Room> roomRepo)
    : ICreateOrUpdateCommand<UpdateRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault();

        if (room == null)
            throw new RoomNotFoundException();

        room.Name = request.Name;
        room.RoomBooks = request.RoomBooks;

        await roomRepo.Update(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room updated successfully.");
    }
}