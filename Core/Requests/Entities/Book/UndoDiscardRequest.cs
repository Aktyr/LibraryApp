namespace LibApp.Core.Requests.Entities.Book;

public class UndoDiscardRequest : IAddOrUpdateRequest
{
    public Guid DiscardId { get; set; }
    public string UndoReason { get; set; } = string.Empty;  // Причина отмены
    public string? ApprovedBy { get; set; }
}

