using LibraryApp.Models;
using LibraryApp.Controllers;
using System.ComponentModel;
using System.Diagnostics.Metrics;

namespace LibraryApp
{
    /// <summary>
    /// Логика взаимодействия для WindowNewRoom.xaml
    /// </summary>
    public partial class WindowNewRoom : Window, INotifyPropertyChanged
    {
        private Room _room = new("Имя комнаты");
        public Room Room
        {
            get => _room;
            set
            {
                _room = value;
                PropertyChanged?.Invoke(this, new(nameof(Room)));
            }
        }

        private RoomBook _roomBook = new("Имя комнаты");
        public RoomBook RoomBook
        {
            get => _roomBook;
            set
            {
                _roomBook = value;
                PropertyChanged?.Invoke(this, new(nameof(RoomBook)));
            }
        }
        public WindowNewRoom() => InitializeComponent();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_AddRoom(object sender, RoutedEventArgs e)
        {
            var context = new RoomDataContext();
            context.Rooms.Add(Room);
            context.SaveChanges();
            MessageBox.Show("Комната добавлена");            
        }

        private void Button_AddRoomBook(object sender, RoutedEventArgs e) 
        {
            // прикрутить обращение к Room через ComboBox
            string name = textBox_RoomBook.Text;
            RoomBook roomBook = new(name);

            try
            {
                Room.RoomBooks.Add(roomBook);
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Room не выбран \n\n{ex}");
                return;
            }           

            MessageBox.Show("Комната добавлена");
        }       
     
        
    }
}
