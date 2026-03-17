namespace LibApp.Core.Requests.Entities.User;

public class CreateUserRequest : IAddOrUpdateRequest
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
}
