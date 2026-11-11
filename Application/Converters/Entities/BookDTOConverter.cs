namespace LibApp.Application.Converters;

public class BookDTOConverter : BaseConverter<Book, BookDTO>
{
    private readonly RoomBookDTOConverter _roomBookConverter = new();

    public override BookDTO ToDto(Book entity)
    {
        var dto = base.ToDto(entity);
        dto = dto with
        {
            RoomBook = entity.RoomBook?.Select(rb => _roomBookConverter.ToDto(rb)).ToList() ?? [],
            Images = entity.Images?.Select(i => new BookImageDTO(i.Id.Value, i.BookId.Value, i.Url, i.SortOrder, i.IsMain)).ToList() ?? []
        };
        return dto;
    }

    public override Book ToEntity(BookDTO dto)
    {
        var entity = base.ToEntity(dto);
        if (dto.Images != null)
        {
            entity.Images = dto.Images.Select(i => new BookImage
            {
                Id = new Id(i.Id),
                BookId = new Id(i.BookId),
                Url = i.Url,
                SortOrder = i.SortOrder,
                IsMain = i.IsMain
            }).ToList();
        }
        return entity;
    }
}