namespace LibApp.Core.Requests.User;

public class DeleteUserRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}
