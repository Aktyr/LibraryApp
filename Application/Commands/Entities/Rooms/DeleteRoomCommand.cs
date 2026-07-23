namespace LibApp.Application.Commands.Entities.Rooms;

public class DeleteRoomCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();
        if (room.RoomBooks?.Any() == true)
            throw new RoomDeletionException("Not possible to delete a room containing books");

        await roomRepo.Remove(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<Room>();
    }
}