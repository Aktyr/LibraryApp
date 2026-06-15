namespace LibApp.Core.Responses.Entities;
public record DiscardedBookResponse(string Status, string Message, DiscardedBookDTO[] Discarded) : IGetResponse;