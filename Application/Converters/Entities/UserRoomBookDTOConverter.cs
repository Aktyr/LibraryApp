namespace LibApp.Application.Converters;

public class UserRoomBookDTOConverter : BaseConverter<UserRoomBook, UserRoomBookDTO>
{
    public override UserRoomBook ToEntity(UserRoomBookDTO dto)
    {
        var entity = base.ToEntity(dto);

        if (dto.UserId != Guid.Empty && entity.User == null)
            entity.User = StubById<User>(dto.UserId);

        if (dto.RoomBookId != Guid.Empty && entity.RoomBook == null)
            entity.RoomBook = StubById<RoomBook>(dto.RoomBookId);

        return entity;
    }
}