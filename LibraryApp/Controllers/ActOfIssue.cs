using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class ActOfIssue(User user, Book book, Room room)
    {
     
        public User user { get; set; } // нужнно id
        public Book book { get; set; } // нужнно id
        public Room room { get; set; } // ?
        public DateTime issue {  get; set; }
        public DateTime deadline { get; set; }


    }
}
