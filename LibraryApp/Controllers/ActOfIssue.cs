using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class ActOfIssue(User user, Book book, RoomBook roomBook)
    {

        public User User { get; set; } = user; 
        public Book Book { get; set; } = book; 
        public RoomBook RoomBook { get; set; } = roomBook; 
        public DateTime Issue {  get; set; } = DateTime.Today;
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(14);


    }
}
