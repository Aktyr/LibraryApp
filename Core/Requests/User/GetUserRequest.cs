namespace LibApp.Core.Requests.User;

public class GetUserRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
