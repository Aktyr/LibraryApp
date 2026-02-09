namespace LibApp.Application.Converters;

public class RoomBookDTOConverter : BaseConverter<RoomBook, RoomBookDTO>
{
    protected override Dictionary<string, Func<object?, object?>> ToDtoConverters
    {
        get
        {
            var converters = base.ToDtoConverters;

            // Удаляем неправильные конвертеры
            converters.Remove("RoomId");
            converters.Remove("BookId");

            return converters;
        }
    }

    public override RoomBookDTO ToDto(RoomBook entity)
    {
        // Создаем базовый DTO
        var dto = base.ToDto(entity);

        // Вручную устанавливаем RoomId и BookId
        var roomId = entity.Room?.Id.Value ?? Guid.Empty;
        var bookId = entity.Book?.Id.Value ?? Guid.Empty;

        // Для record используем with выражение
        return dto with
        {
            RoomId = roomId,
            BookId = bookId
        };
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