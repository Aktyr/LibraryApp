namespace LibApp.Application.Entities.Rooms.Delete;

public class DeleteRoomCommand(IRepository<Room> roomRepo)
    : IDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault();

        if (room == null)
            throw new RoomNotFoundException();

        if (room.RoomBooks != null)
            throw new RoomDeletionException("Not possible to delete a room containing books");

        await roomRepo.Remove(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room deleted successfully.");
    }
}