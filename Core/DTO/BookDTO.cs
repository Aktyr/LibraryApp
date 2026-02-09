namespace LibApp.Core.DTO;

public record BookDTO(Guid Id,
                      string Title,
                      string Author,
                      int Year,
                      string Publisher) // может нужно ICollection<RoomBook>
{ public BookDTO() : this(default, default!, default!, default, default!) { } }
