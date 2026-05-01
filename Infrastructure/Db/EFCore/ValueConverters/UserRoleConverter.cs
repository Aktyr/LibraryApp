namespace LibApp.Infrastructure.Db.EFCore.ValueConverters;

public class UserRoleConverter : ValueConverter<UserRole, string>
{
    public UserRoleConverter() : base(role => role.ToString(),
                                      str => Enum.Parse<UserRole>(str)) { }
}