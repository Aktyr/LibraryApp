namespace LibApp.Core.Requests.Borrowing;

public class ExtendDeadlineRequest : IAddOrUpdateRequest
{
    public Guid UserRoomBookId { get; set; }
    public int ExtraDays { get; set; }  
    public DateTime NewDeadline => DateTime.UtcNow.AddDays(ExtraDays); 
}