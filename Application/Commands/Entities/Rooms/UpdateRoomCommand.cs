namespace LibApp.Application.Commands.Entities.Rooms;

public class UpdateRoomCommand(
    IUnitOfWork unitOfWork, 
    RoomValidatorAsync roomValidator, 
    IConverter<Room, RoomDTO> roomConverter) : ICreateOrUpdateCommand<UpdateRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault() ?? throw new RoomNotFoundException();

        // Временное DTO для валидации
        RoomDTO roomDto = new(Guid.NewGuid(),
                              request.Name, []);

        var roomForValidation = roomConverter.ToEntity(roomDto);

        var validationResult = await roomValidator.ValidateAsync(roomForValidation, cancellationToken);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка уникальности имени, если изменилось
        if (room.Name != request.Name &&
            (await roomRepo.Get(x => x.Name == request.Name, cancellationToken)).Any())
            throw new RoomExistsException(request.Name);

        // Обновляем только если валидация прошла
        room.Name = request.Name;
        room.RoomBooks = request.RoomBooks;

        await roomRepo.Update(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Updated<Room>();
    }
}