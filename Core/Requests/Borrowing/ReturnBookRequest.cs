namespace LibApp.Core.Requests.Borrowing;

public class ReturnBookRequest : IAddOrUpdateRequest
{
    public Guid UserRoomBookId {  get; set; }
}
