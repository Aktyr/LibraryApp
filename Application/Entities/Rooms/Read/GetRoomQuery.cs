namespace LibApp.Application.Entities.Rooms.Read;

public class GetRoomQuery(IRepository<Room> roomRepo)
    : IGetQuery<GetRoomRequest, RoomResponse>
{
    public async Task<RoomResponse?> Execute(GetRoomRequest request, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var room = rooms.FirstOrDefault();

        if (room == null)
            throw new RoomNotFoundException(); // todo fix!!! -- Исправлено?

        var roomDTO = new RoomDTO(
            room.Id.Value,
            room.Name,
            [.. room.RoomBooks.Select(rb => new RoomBookDTO(
            rb.Id.Value,
            rb.Room?.Id.Value ?? Guid.Empty, // Защита от null
            rb.Book?.Id.Value ?? Guid.Empty,
            rb.BookCount
        ))]
        );

        return new RoomResponse("Ok", roomDTO);
    }
}