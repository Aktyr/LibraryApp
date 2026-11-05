namespace LibApp.Core.Entities;

public class Book : IEntity
{
    public Id Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;

    /// <summary>Аннотация. Необязательна; ограничение длины — в BookPolicy.MaxDescriptionLength.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>URL обложки (http/https). Необязателен; формат проверяется в BookPolicy.</summary>
    public string CoverUrl { get; set; } = string.Empty;

    public bool IsDiscarded { get; set; }
    public virtual ICollection<RoomBook> RoomBook { get; set; } = [];

    public override string ToString() =>
        $"Название: {Title}\nАвтор: {Author}\nГод: {Year}";
}