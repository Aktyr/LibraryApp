namespace LibApp.Core.Requests.Entities.User;

public class GetUserRequest : IGetRequest
{
    public Id Id { get; set; } = null!;
}
