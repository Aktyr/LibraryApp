namespace LibApp.Application.Commands.Entities.Rooms;

public class DeleteRoomCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();
        var rooms = await roomRepo.GetAsync(
            r => r.Id.Value == request.Id.Value,
            includePaths: IncludePaths.Room.RoomBooks); 
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();
        if (room.RoomBooks?.Any() == true)
            throw new RoomDeletionException("Not possible to delete a room containing books");

        await roomRepo.RemoveAsync(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<Room>();
    }
}