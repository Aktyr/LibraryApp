using LibraryApp.Models;

namespace LibraryApp.Controllers
{

    internal class BookDataContext // Хранение книг в файле
    {

        public BookDataContext()
        {

            string json = File.ReadAllText("books.json");
            Books = JArray.Parse(json).ToObject<List<Book>>();
        }
        public List<Book> Books { get; set; }




        public void SaveChanges()
        {
            JArray books = JArray.FromObject(Books);
            File.WriteAllText("books.json", books.ToString());
        }
    }
}