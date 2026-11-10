namespace LibApp.Application.Commands.Entities.Rooms;

public class DeleteRoomCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedDeleteCommand<DeleteRoomRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = UnitOfWork.GetRepository<Room>();

        var rooms = await roomRepo.GetAsync(
            r => r.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Room.RoomBooks);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();

        if (room.RoomBooks?.Any() == true)
            throw new RoomDeletionException("Not possible to delete a room containing books");

        await roomRepo.RemoveAsync(room, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var audit = new AuditEntry(
            AuditActionType.Delete,
            nameof(Room),
            room.Id.Value,
            $"Удалена комната «{room.Name}»");

        return (ResponseFactory.Deleted<Room>(), audit);
    }
}