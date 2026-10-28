namespace LibApp.Core.Entities;

public class Book : IEntity
{
    public Id Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public bool IsDiscarded { get; set; }
    //todo в ТЗ указано добавить статус подписки, какие виды подписок нужны? 
    public virtual ICollection<RoomBook> RoomBook { get; set; } = [];

    public override string ToString() =>
        $"Название: {Title}\nАвтор: {Author}\nГод: {Year}";
}