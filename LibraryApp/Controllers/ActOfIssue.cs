using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class ActOfIssue(User user, Book book, Room room) // может переместить в модели...
    {
     
        public User user { get; set; } // нужнно id
        public Book book { get; set; } // нужнно id
        public Room room { get; set; } // ?
        public DateTime issue {  get; set; } = DateTime.Today;
        public DateTime deadline { get; set; }


    }
}
