namespace LibApp.Core.DTO.Entities;

public record BookDTO(Guid Id,
                      string Title,
                      string Author,
                      int Year,
                      string Publisher,
                      string Genre,
                      ICollection<RoomBookDTO> RoomBook,
                      bool IsDiscarded = false,
                      string Description = "",
                      ICollection<BookImageDTO>? Images = null)
{
    public BookDTO() : this(default, default!, default!, default!, default!, default!, default!, default, "", []) { }

    /// <summary>Удобный доступ к основной обложке для карточек каталога.</summary>
    public string? MainCoverUrl => Images?.FirstOrDefault(i => i.IsMain)?.Url
                                   ?? Images?.FirstOrDefault()?.Url;
}