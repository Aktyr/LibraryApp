using LibraryApp.Models;
using LibraryApp.Controllers;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Collections.ObjectModel;
using System.Windows.Controls;

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

        private readonly BookDataContext _context;

        public WindowNewRoom()
        {
            InitializeComponent();
            _context = new BookDataContext();
            this.DataContext = _context;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Button_AddRoom(object sender, RoutedEventArgs e)
        {
            ObservableCollection<Room> a;

            var context = new RoomDataContext();
            context.Rooms.Add(Room);
            context.SaveChanges();

            var selectedItems = dataGrid.SelectedItems;

            foreach (var item in selectedItems)
            {
                ProcessItem(item); 
            }


            MessageBox.Show("Комната добавлена");

        }        
                
        private void ProcessItem(object item)
        {           
            var myObject = item as Book;

            RoomBook RoomBook = new(Room, myObject);
            var context = new RoomBookDataContext();
            context.RoomBooks.Add(RoomBook);
            context.SaveChanges();
        }

    }
}
