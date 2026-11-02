namespace LibApp.Application.Commands.Entities.Rooms;

public class DeleteRoomCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : IDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();

        var rooms = await roomRepo.GetAsync(
            r => r.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Room.RoomBooks);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();

        if (room.RoomBooks?.Any() == true)
            throw new RoomDeletionException("Not possible to delete a room containing books");

        var deletedName = room.Name;
        var deletedId = room.Id.Value;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await roomRepo.RemoveAsync(room, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Delete,
                nameof(Room),
                deletedId,
                details: $"Удалена комната «{deletedName}»",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Deleted<Room>();
    }
}