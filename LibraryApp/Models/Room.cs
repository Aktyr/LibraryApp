namespace LibraryApp.Models
{
    internal class Room(string name)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = name;
        public ICollection<RoomBook> Books { get; set; }
    }
}
