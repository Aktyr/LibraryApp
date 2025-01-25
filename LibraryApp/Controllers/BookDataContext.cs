using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class BookDataContext // Хранение книг в файле
    {
        private const string json = "books.json";

        public BookDataContext()
        {
            // Проверяем существование файла
            if (!File.Exists(BookDataContext.json))
            {
                // Создаем новый файл с пустым массивом
                File.WriteAllText(BookDataContext.json, "[]");
            }

            var json = File.ReadAllText(BookDataContext.json);
            Books = JArray.Parse(json).ToObject<List<Book>>() ?? [];
        }
        public List<Book> Books { get; init; }
        public void SaveChanges()
        {
            JArray books = JArray.FromObject(Books);
            File.WriteAllText(json, books.ToString());
        }
    }
}