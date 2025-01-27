namespace LibraryApp.Models
{
    public class RoomBook(string name)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = name;
        public List<Book> Books { get; set; }

        public override string ToString() => $"Книжная комната: {Name}, Кол-во книг: {Books.Count}";

    }
}
