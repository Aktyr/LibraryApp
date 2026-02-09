namespace LibApp.Application.Converters;

internal class RoomBookDTOConverter : BaseConverter<RoomBook, RoomBookDTO>
{
    protected override Dictionary<string, Func<object?, object?>> ToDtoConverters
    {
        get
        {
            var converters = base.ToDtoConverters;

            converters["RoomId"] = value => ((RoomBook)value!).Room?.Id.Value ?? Guid.Empty;
            converters["BookId"] = value => ((RoomBook)value!).Book?.Id.Value ?? Guid.Empty;

            return converters;
        }
    }
    public override RoomBook ToEntity(RoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);

        if (dto.RoomId != Guid.Empty)
            entity.Room = new Room { Id = new Id(dto.RoomId) };

        if (dto.BookId != Guid.Empty)
            entity.Book = new Book { Id = new Id(dto.BookId) };

        return entity;
    }

}

