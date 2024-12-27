namespace LibraryApp.Models
{
    internal class Room(string name)
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string name { get; set; } = name;

    }
}
