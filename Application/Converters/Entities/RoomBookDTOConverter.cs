namespace LibApp.Application.Converters;

public class RoomBookDTOConverter : BaseConverter<RoomBook, RoomBookDTO>
{
    public override RoomBookDTO ToDto(RoomBook entity)
    {
        var dto = base.ToDto(entity);

        if (entity.Room != null)
            dto = dto with { RoomId = entity.Room.Id.Value };

        if (entity.Book != null)
            dto = dto with { BookId = entity.Book.Id.Value };

        return dto;
    }

    public override RoomBook ToEntity(RoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);

        if (dto.RoomId != Guid.Empty && entity.Room == null)
            entity.Room = new Room { Id = new Id(dto.RoomId) };

        if (dto.BookId != Guid.Empty && entity.Book == null)
            entity.Book = new Book { Id = new Id(dto.BookId) };

        return entity;
    }
}