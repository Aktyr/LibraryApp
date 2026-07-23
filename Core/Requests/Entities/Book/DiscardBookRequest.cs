namespace LibApp.Core.Requests.Entities.Book;

public class DiscardBookRequest : IAddOrUpdateRequest
{
    public Guid BookId { get; set; }
    public int Amount { get; set; }
    public DiscardReason DiscardReason { get; set; }
    public string? ApprovedBy { get; set; }
    public decimal? CompensationAmount { get; set; }
}