using LibraryApp.Models;

namespace LibraryApp.Controllers;

public class UserRoomBookDataContext
{
    private const string json = "userRoomBooks.json";

    public UserRoomBookDataContext()
    {
        // Проверяем существование файла
        if (!File.Exists(UserRoomBookDataContext.json))
        {
            // Создаем новый файл с пустым массивом
            File.WriteAllText(UserRoomBookDataContext.json, "[]");
        }

        var json = File.ReadAllText(UserRoomBookDataContext.json);
        UserRoomBooks = JArray.Parse(json).ToObject<List<UserRoomBook>>() ?? [];
    }
    public List<UserRoomBook> UserRoomBooks { get; init; }
    public void SaveChanges()
    {
        // Используем настройки сериализатора для обработки циклических ссылок
        var serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        var userRoomBooks = JsonConvert.SerializeObject(UserRoomBooks, Formatting.Indented, serializerSettings);
        File.WriteAllText(json, userRoomBooks.ToString());
    }

}
