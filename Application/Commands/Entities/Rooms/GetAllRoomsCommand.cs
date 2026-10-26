namespace LibApp.Application.Commands.Entities.Rooms;

public class GetAllRoomsCommand(IUnitOfWork unitOfWork, IConverter<Room, RoomDTO> RoomConverter)
    : IGetQuery<EmptyRequest, RoomResponse>, ICommand
{
    public async Task<RoomResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken) //todo не используется ct
    {
        var roomRepo = unitOfWork.GetRepository<Room>();
        var rooms = await roomRepo.GetAsync(includePaths: [IncludePaths.Room.RoomBooksBook]);
        var roomDTOs = rooms.Select(room => RoomConverter.ToDto(room)).ToArray();

        return ResponseFactory.List<Room, RoomDTO, RoomResponse>(roomDTOs);
    }
}