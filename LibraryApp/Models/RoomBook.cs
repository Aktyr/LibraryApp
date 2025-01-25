namespace LibraryApp.Models
{
    internal class RoomBook(string name)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = name;
        public ICollection<Book> books { get; set; }
    }
}
