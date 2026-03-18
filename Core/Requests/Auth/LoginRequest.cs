namespace LibApp.Core.Requests.Auth;

public class LoginRequest : IGetRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
