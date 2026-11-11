namespace LibApp.Core.Entities;

public class BookImage : IEntity
{
    public Id Id { get; set; }

    public Id BookId { get; set; }
    public virtual Book Book { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    /// <summary>Порядок в галерее. Меньше — раньше.</summary>
    public int SortOrder { get; set; }

    /// <summary>Обложка карточки каталога. Гарантируется одна на книгу в валидаторе (BookPolicy).</summary>
    public bool IsMain { get; set; }
}