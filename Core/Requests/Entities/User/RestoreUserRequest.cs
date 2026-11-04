namespace LibApp.Core.Requests.Entities.User;

public class RestoreUserRequest : IAddOrUpdateRequest
{
    public Id Id { get; set; } = null!;
}