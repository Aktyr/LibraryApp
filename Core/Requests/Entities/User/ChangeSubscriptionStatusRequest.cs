namespace LibApp.Core.Requests.Entities.User;

public class ChangeSubscriptionStatusRequest : IAddOrUpdateRequest
{
    public Id UserId { get; set; } = null!;
    public SubscriptionStatus NewStatus { get; set; }
}