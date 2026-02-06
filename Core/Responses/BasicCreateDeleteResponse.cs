namespace LibApp.Core.Responses;

// Только для Create, Delete, Update
public record BasicCreateDeleteResponse(string Status, string Message) : IAddOrUpdateResponse, IDeleteResponse;