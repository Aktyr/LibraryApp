namespace LibApp.Application.Converters;

public class RoomDTOConverter : BaseConverter<Room, RoomDTO>
{
    private readonly RoomBookDTOConverter _roomBookConverter = new();

    public override RoomDTO ToDto(Room entity)
    {
        var dto = base.ToDto(entity);

        // Конвертируем RoomBooks в RoomBookDTO
        if (entity.RoomBooks != null && entity.RoomBooks.Any())
        {
            var roomBookDTOs = entity.RoomBooks
                .Select(rb => _roomBookConverter.ToDto(rb))
                .ToList();

            // Для record используем with выражение
            dto = dto with { RoomBook = roomBookDTOs };
        }
        else
        {
            // Инициализируем пустую коллекцию
            dto = dto with { RoomBook = new List<RoomBookDTO>() };
        }

        return dto;
    }
}
