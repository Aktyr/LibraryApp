namespace LibApp.Core.DTO.Entities;

public record BookImageDTO(
    Guid Id,
    Guid BookId,
    string Url,
    int SortOrder,
    bool IsMain)
{
    public BookImageDTO() : this(default, default, default!, default, default) { }
}