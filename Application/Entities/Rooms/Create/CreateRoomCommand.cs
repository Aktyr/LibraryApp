namespace LibApp.Application.Entities.Rooms.Create;

public class CreateRoomCommand(IRepository<Room> roomRepo)
    : ICreateOrUpdateCommand<CreateRoomRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = new Room
        {
            Name = request.Name,
            RoomBooks = []
        };
        if ((await roomRepo.Get(x => x.Name == request.Name, cancellationToken)).Any())
            throw new RoomExistsException(request.Name);

        await roomRepo.Add(room, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Room is created.");
    }
}
