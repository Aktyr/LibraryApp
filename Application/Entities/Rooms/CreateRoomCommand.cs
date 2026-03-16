using LibApp.Application.Validation.Entities;

namespace LibApp.Application.Entities.Rooms;

public class CreateRoomCommand(IRepository<Room> roomRepo, RoomValidatorAsync roomValidator, IConverter<Room, RoomDTO> roomConverter)
    : ICreateOrUpdateCommand<CreateRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        // Создание
        RoomDTO roomDto = new(Guid.NewGuid(),
                              request.Name, []);
        var room = roomConverter.ToEntity(roomDto);


        // Валидация
        var validationResult = await roomValidator.ValidateAsync(room, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка существования комнаты с таким именем
        if ((await roomRepo.Get(x => x.Name == request.Name, cancellationToken)).Any())
            throw new RoomExistsException(request.Name);

        // Добавление
        await roomRepo.Add(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room is created.");
    }
}