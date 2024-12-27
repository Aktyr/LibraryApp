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
            Users = JArray.Parse(json).ToObject<List<User>>();
        }
        public List<User> Users { get; set; }
        public void SaveChanges()
        {
            JArray users = JArray.FromObject(Users);
            File.WriteAllText(json, users.ToString());
        }
    }
}


