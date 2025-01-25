using LibraryApp.Models;

namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewRoom.xaml
    /// </summary>
    public partial class WindowNewRoom : Window
    {
        public WindowNewRoom() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = textBox_RoomName.Text;

            Room room = new(name);

            MessageBox.Show($"Это ещё предстоит доделать \n\nНазвание комнаты: {room.Name}\nid комнаты: {room.Id}");
            Close();
        }
    }
}
