namespace LibApp.Application.Entities.Rooms.Read;

public class GetAllRooms(IRepository<Room> roomRepo)
    : IGetQuery<EmptyRequest, RoomsListResponse>
{
    public async Task<RoomsListResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.GetWithoutTracking(cancellationToken);
        var roomDTOs = rooms.Select(room => 
            new RoomDTO( 
                room.Id.Value,
                room.Name,
                []    // todo maybe its too harsh
            )
        ).ToArray();

        return new RoomsListResponse("Ok", "List of rooms issued successfully", roomDTOs);
    }
}