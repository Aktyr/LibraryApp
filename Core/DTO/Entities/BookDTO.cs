namespace LibApp.Core.DTO.Entities;

public record BookDTO(Guid Id,
                      string Title,
                      string Author,
                      int Year,
                      string Publisher,
                      string Genre,
                      ICollection<RoomBookDTO> RoomBook)
{ public BookDTO() : this(default, default!, default!, default!, default!, default!, default!) { } }
