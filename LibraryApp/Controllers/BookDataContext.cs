using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    public class BookDataContext // Хранение книг в файле
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
            // Используем настройки сериализатора для обработки циклических ссылок
            var serializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var books = JsonConvert.SerializeObject(Books, Formatting.Indented, serializerSettings);
            File.WriteAllText(json, books.ToString());
        }
    }
}