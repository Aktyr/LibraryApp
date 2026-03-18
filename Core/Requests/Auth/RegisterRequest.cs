namespace LibApp.Core.Requests.Auth;

public class RegisterRequest : IAddOrUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
}
