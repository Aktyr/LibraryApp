namespace LibApp.Application.Converters;

public class UserRoomBookDTOConverter : BaseConverter<UserRoomBook, UserRoomBookDTO>
{
    protected override Dictionary<string, Func<object?, object?>> ToDtoConverters
    {
        get
        {
            var converters = base.ToDtoConverters;

            // Удаляем неправильные конвертеры
            converters.Remove("UserId");
            converters.Remove("RoomBookId");

            return converters;
        }
    }

    public override UserRoomBookDTO ToDto(UserRoomBook entity)
    {
        // Создаем базовый DTO
        var dto = base.ToDto(entity);

        // Вручную устанавливаем UserId и RoomBookId из навигационных свойств
        var userId = entity.User?.Id.Value ?? Guid.Empty;
        var roomBookId = entity.RoomBook?.Id.Value ?? Guid.Empty;

        // Для record используем with выражение
        return dto with
        {
            UserId = userId,
            RoomBookId = roomBookId
        };
    }

    public override UserRoomBook ToEntity(UserRoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);

        if (dto.UserId != Guid.Empty)
            entity.User = new User { Id = new Id(dto.UserId) };

        if (dto.RoomBookId != Guid.Empty)
            entity.RoomBook = new RoomBook { Id = new Id(dto.RoomBookId) };

        return entity;
    }
}