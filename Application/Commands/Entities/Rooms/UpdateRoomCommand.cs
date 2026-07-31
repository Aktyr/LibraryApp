namespace LibApp.Application.Commands.Entities.Rooms;

public class UpdateRoomCommand(
    IUnitOfWork unitOfWork,
    RoomValidatorAsync roomValidator,
    IRoomBookSynchronizer roomBookSynchronizer,
    ILogger<UpdateRoomCommand> logger)
    : ICreateOrUpdateCommand<UpdateRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();

        var room = (await roomRepo.GetAsync(
            r => r.Id.Value == request.Id.Value,
            includePaths: IncludePaths.Room.RoomBooksBook)).FirstOrDefault()
            ?? throw new RoomNotFoundException();

        // Валидация имени и проверка уникальности
        await ValidateRoomNameAsync(request.Name, cancellationToken);
        if (room.Name != request.Name && await IsNameTakenAsync(request.Name, cancellationToken))
            throw new RoomExistsException(request.Name);

        room.Name = request.Name;

        // Транзакция охватывает синхронизацию и сохранение
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await roomBookSynchronizer.SynchronizeAsync(room, request.RoomBookDTO, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ResponseFactory.Updated<Room>();
    }
    private async Task ValidateRoomNameAsync(string name, CancellationToken ct)
    {
        var tempRoom = new Room { Name = name };
        var result = await roomValidator.ValidateAsync(tempRoom, ct);
        if (!result.IsValid)
            throw new LibValidationException { ExceptionDetails = result.Errors };
    }

    private async Task<bool> IsNameTakenAsync(string name, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<Room>();
        var existing = await repo.GetWithoutTrackingAsync(r => r.Name == name, ct);
        return existing.Any();
    }
}