namespace LibApp.Core.Authorization;

public class UserManagementPolicy : IUserManagementPolicy
{
    // Admin — всё. Librarian — только Reader. Reader — ничего.
    // Create и Edit проверяют пару (actor, target), Delete — только actor.
    public bool CanCreate(UserRole actor, UserRole targetRole) => actor switch
    {
        UserRole.Admin => true,
        UserRole.Librarian => targetRole == UserRole.Reader,
        _ => false
    };

    public bool CanEdit(UserRole actor, UserRole targetRole) => actor switch
    {
        UserRole.Admin => true,
        UserRole.Librarian => targetRole == UserRole.Reader,
        _ => false
    };

    public bool CanDelete(UserRole actor) => actor == UserRole.Admin;
}