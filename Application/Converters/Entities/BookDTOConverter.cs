namespace LibApp.Application.Converters;

public class BookDTOConverter : BaseConverter<Book, BookDTO> 
{
    private readonly RoomBookDTOConverter _roomBookConverter = new();

    public override BookDTO ToDto(Book entity)
    {
        var dto = base.ToDto(entity);
        if (entity.RoomBook != null)
        {
            var roomBookDTOs = entity.RoomBook
                .Select(rb => _roomBookConverter.ToDto(rb))
                .ToList();
            dto = dto with { RoomBook = roomBookDTOs };
        }
        else
        {
            dto = dto with { RoomBook = [] };
        }
        return dto;
    }

} 