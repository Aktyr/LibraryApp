namespace LibApp.Application.Converters;

internal class UserRoomBookDTOConverter : BaseConverter<UserRoomBook, UserRoomBookDTO>
{
    protected override Dictionary<string, Func<object?, object?>> ToDtoConverters
    {
        get
        {
            var converters = base.ToDtoConverters;

            converters["UserId"] = value => (value as UserRoomBook)?.User?.Id.Value ?? Guid.Empty;
            converters["RoomBookId"] = value => (value as UserRoomBook)?.RoomBook?.Id.Value ?? Guid.Empty;

            return converters;
        }
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