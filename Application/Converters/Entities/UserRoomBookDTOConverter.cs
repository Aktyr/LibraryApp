namespace LibApp.Application.Converters;

public class UserRoomBookDTOConverter : BaseConverter<UserRoomBook, UserRoomBookDTO>
{
    public override UserRoomBookDTO ToDto(UserRoomBook entity)
    {
        var dto = base.ToDto(entity);

        if (entity.User != null)
            dto = dto with { UserId = entity.User.Id.Value };

        if (entity.RoomBook != null)
            dto = dto with { RoomBookId = entity.RoomBook.Id.Value };

        return dto;
    }

    public override UserRoomBook ToEntity(UserRoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);

        if (dto.UserId != Guid.Empty && entity.User == null)
            entity.User = new User { Id = new Id(dto.UserId) };

        if (dto.RoomBookId != Guid.Empty && entity.RoomBook == null)
            entity.RoomBook = new RoomBook { Id = new Id(dto.RoomBookId) };

        return entity;
    }
}