using LibraryApp.Models;

namespace LibraryApp.Controllers;

public class RoomDataContext
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
        // Используем настройки сериализатора для обработки циклических ссылок
        var serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        var rooms = JsonConvert.SerializeObject(Rooms, Formatting.Indented, serializerSettings);
        File.WriteAllText(json, rooms.ToString());
    }
}


