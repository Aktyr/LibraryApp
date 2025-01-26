using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class RoomDataContext
    {
        private const string json = "rooms.json";

        public RoomDataContext()
        {
            // Проверяем существование файла
            if (!File.Exists(RoomDataContext.json))
            {
                // Создаем новый файл с пустым массивом
                File.WriteAllText(RoomDataContext.json, "[]");
            }

            var json = File.ReadAllText(RoomDataContext.json);
            Rooms = JArray.Parse(json).ToObject<List<Room>>() ?? [];
        }
        public List<Room> Rooms { get; init; }
        public void SaveChanges()
        {
            JArray rooms = JArray.FromObject(Rooms);
            File.WriteAllText(json, rooms.ToString());
        }
    }
}


