using LibApp.Core.DTO.Entities;
using LibApp.Core.Responses.Entities;

namespace LibApp.Application.Commands.Entities.Rooms;

public class GetAllRoomsCommand(IRepository<Room> roomRepo, IConverter<Room, RoomDTO> RoomConverter)
    : IGetQuery<EmptyRequest, RoomResponse>
{
    public async Task<RoomResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var rooms = await roomRepo.GetWithoutTracking(cancellationToken);
        var roomDTOs = rooms.Select(room => RoomConverter.ToDto(room)).ToArray();

        return new RoomResponse("Ok", "List of rooms issued successfully.", roomDTOs);
    }
}