using LibApp.Core.DTO.Entities;

namespace LibApp.Core.Responses.Entities;
public record BookResponse(string Status, string Message, BookDTO[] Book) : IGetResponse;