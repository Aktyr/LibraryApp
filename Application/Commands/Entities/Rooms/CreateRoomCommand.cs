namespace LibApp.Application.Commands.Entities.Rooms;

public class CreateRoomCommand(
    IUnitOfWork unitOfWork,
    RoomValidatorAsync roomValidator,
    IConverter<Room, RoomDTO> roomConverter,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<CreateRoomRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = UnitOfWork.GetRepository<Room>();

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

        await roomRepo.AddAsync(room, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var audit = new AuditEntry(
            AuditActionType.Create,
            nameof(Room),
            room.Id.Value,
            $"Создана комната «{room.Name}»");

        return (ResponseFactory.Created<Room>(), audit);
    }
}