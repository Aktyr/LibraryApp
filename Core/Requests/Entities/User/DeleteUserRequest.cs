namespace LibApp.Core.Requests.Entities.User;

public class DeleteUserRequest : IDeleteRequest
{
    public Id Id { get; set; } = null!;
}
