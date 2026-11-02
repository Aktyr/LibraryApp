namespace LibApp.Application.Commands.Entities.Rooms;

public class CreateRoomCommand(
    IUnitOfWork unitOfWork,
    RoomValidatorAsync roomValidator,
    IConverter<Room, RoomDTO> roomConverter,
    IAuditService auditService) : ICreateOrUpdateCommand<CreateRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();

        // Создание
        RoomDTO roomDto = new(Guid.NewGuid(),
                              request.Name, []);
        var room = roomConverter.ToEntity(roomDto);

        // Валидация
        var validationResult = await roomValidator.ValidateAsync(room, cancellationToken);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка существования комнаты с таким именем
        if (await roomRepo.AnyAsync(r => r.Name == request.Name, cancellationToken))
            throw new RoomExistsException(request.Name);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await roomRepo.AddAsync(room, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Create,
                nameof(Room),
                room.Id.Value,
                details: $"Создана комната «{room.Name}»",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Created<Room>();
    }
}