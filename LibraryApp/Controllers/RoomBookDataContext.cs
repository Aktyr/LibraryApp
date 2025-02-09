using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class RoomBookDataContext
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
            JArray roomBooks = JArray.FromObject(RoomBooks);
            File.WriteAllText(json, roomBooks.ToString());
        }
    }
}


