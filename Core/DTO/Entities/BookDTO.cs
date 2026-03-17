namespace LibApp.Core.DTO.Entities;

public record BookDTO(Guid Id,
                      string Title,
                      string Author,
                      int Year,
                      string Publisher,
                      ICollection<RoomBook> RoomBook)
{ public BookDTO() : this(default, default!, default!, default, default!, default!) { } }
