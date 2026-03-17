namespace LibApp.Core.Requests.Entities.User;

public class UpdateUserRequest : IAddOrUpdateRequest
{
    public Id Id { get; set; } = null!;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
}
