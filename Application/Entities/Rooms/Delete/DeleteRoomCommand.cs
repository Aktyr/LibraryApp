namespace LibApp.Application.Entities.Rooms.Delete;

public class DeleteRoomCommand(IRepository<Room> roomRepo)
    : IDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault();

        if (room == null)
        {
            return new BasicCreateDeleteResponse("Error", "Room not found.");
        }

        await roomRepo.Remove(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room deleted successfully.");
    }
}