namespace LibApp.Application.Converters;

public class UserDTOConverter : BaseConverter<User, UserDTO>
{
    public override UserDTO ToDto(User entity)
    {
        var dto = base.ToDto(entity);
        return dto with
        {
            ActiveBorrowedCount = entity.RoomBooks?.Count(urb => !urb.IsReturned) ?? 0
        };
    }
}