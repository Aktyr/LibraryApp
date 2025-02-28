namespace LibraryApp.Models;

public class Book(string name, string autor, int year, string publisher)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = name;
    public string Autor { get; set; } = autor;
    public int Year { get; set; } = year;
    public string Publisher { get; set; } = publisher;


    //[JsonIgnore]
    public List<RoomBook> RoomBooks { get; set; } = [];
    public override string ToString() =>
      $@"Название: {Name}
Автор: {Autor}
Год: {Year}\n"; //Копий на складе: {bookCount}";


}
