using LibraryApp.Models;
using static System.Reflection.Metadata.BlobBuilder;

namespace LibraryApp.Controllers
{
    public class RoomBookDataContext
    {
        private const string json = "roomBooks.json";

        public RoomBookDataContext()
        {
            // Проверяем существование файла
            if (!File.Exists(RoomBookDataContext.json))
            {
                // Создаем новый файл с пустым массивом
                File.WriteAllText(RoomBookDataContext.json, "[]");
            }

            var json = File.ReadAllText(RoomBookDataContext.json);
            RoomBooks = JArray.Parse(json).ToObject<List<RoomBook>>() ?? [];
        }
        public List<RoomBook> RoomBooks { get; init; }
        public void SaveChanges()
        {
            // Используем настройки сериализатора для обработки циклических ссылок
            var serializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var roomBooks = JsonConvert.SerializeObject(RoomBooks, Formatting.Indented, serializerSettings);
            File.WriteAllText(json, roomBooks.ToString());
        }
    }
}


