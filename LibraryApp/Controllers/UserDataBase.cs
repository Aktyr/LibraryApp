using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    internal class UserDataBase
    {
        public UserDataBase() 
        {
            var json = File.ReadAllText("users.json");
            Users = JArray.Parse(json).ToObject<List<User>>();
        }
        public List<User> Users { get; set; }

        public void SaveChanges()
        {
            JArray users = JArray.FromObject(Users);
            File.WriteAllText("users.json", users.ToString());
        }
    }
}
