namespace WebAPI.Misc.Authorization;

public class EnumAuthorize : AuthorizeAttribute
{
    public EnumAuthorize(UserRole role) : base() 
        => Roles = role.ToString();
    public EnumAuthorize(params UserRole[] roles) : base() 
        => Roles = string.Join(",", roles.Select(r => r.ToString())); 
}
