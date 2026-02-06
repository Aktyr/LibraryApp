namespace LibApp.Application.Entities.Rooms;

public class UpdateRoomCommand(IRepository<Room> roomRepo, RoomValidatorAsync roomValidator)
    : ICreateOrUpdateCommand<UpdateRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault();

        if (room == null)
            throw new RoomNotFoundException();

        // Валидация
        var validationResult = await roomValidator.ValidateAsync(
            new Room { Name = request.Name },
            cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка уникальности имени, если изменилось
        if (room.Name != request.Name &&
            (await roomRepo.Get(x => x.Name == request.Name, cancellationToken)).Any())
            throw new RoomExistsException(request.Name);

        room.Name = request.Name;
        room.RoomBooks = request.RoomBooks;

        await roomRepo.Update(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room updated successfully.");
    }
}