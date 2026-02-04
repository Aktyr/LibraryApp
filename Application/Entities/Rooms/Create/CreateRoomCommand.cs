namespace LibApp.Application.Entities.Rooms.Create;

public class CreateRoomCommand(IRepository<Room> roomRepo, RoomValidatorAsync roomValidator)
    : ICreateOrUpdateCommand<CreateRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var validationResult = await roomValidator.ValidateAsync(
            new Room { Name = request.Name },
            cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка существования комнаты с таким именем
        if ((await roomRepo.Get(x => x.Name == request.Name, cancellationToken)).Any())
            throw new RoomExistsException(request.Name);

        var room = new Room
        {
            Name = request.Name,
            RoomBooks = []
        };

        await roomRepo.Add(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room is created.");
    }
}