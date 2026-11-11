namespace LibApp.Application.Converters;

public class RoomBookDTOConverter : BaseConverter<RoomBook, RoomBookDTO>
{
    public override RoomBook ToEntity(RoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);
        if (dto.RoomId != Guid.Empty && entity.Room == null)
            entity.Room = StubById<Room>(dto.RoomId);
        if (dto.BookId != Guid.Empty && entity.Book == null)
            entity.Book = StubById<Book>(dto.BookId);
        return entity;
    }
}