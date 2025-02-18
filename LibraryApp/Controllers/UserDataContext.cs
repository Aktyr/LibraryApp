using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class UserDataContext
    {
        private const string json = "users.json";

        public UserDataContext()
        {
            // Проверяем существование файла
            if (!File.Exists(UserDataContext.json))
            {
                // Создаем новый файл с пустым массивом
                File.WriteAllText(UserDataContext.json, "[]");
            }

            var json = File.ReadAllText(UserDataContext.json);
            Users = JArray.Parse(json).ToObject<List<User>>() ?? [];
        }
        public List<User> Users { get; init; }
        public void SaveChanges()
        {
            // Используем настройки сериализатора для обработки циклических ссылок
            var serializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var users = JsonConvert.SerializeObject(Users, Formatting.Indented, serializerSettings);
            File.WriteAllText(json, users.ToString());
        }
    }
}


