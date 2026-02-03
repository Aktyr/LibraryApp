namespace LibApp.Core.Responses;

public record BasicCreateDeleteResponse(string Status, string Message) : IAddOrUpdateResponse, IDeleteResponse;